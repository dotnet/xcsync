// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Marille;
using Microsoft.CodeAnalysis;
using Serilog;
using xcsync.Projects;
using xcsync.Projects.Xcode;
using xcsync.Workers;

namespace xcsync;

class SyncContext (IFileSystem fileSystem, ITypeService typeService, SyncDirection Direction, string projectPath, string targetDir, string framework, ILogger logger, bool Open = false)
	: SyncContextBase (fileSystem, typeService, projectPath, targetDir, framework, logger) {

	public const string FileChannel = "Files";
	public const string SyncChannel = "SyncFromXcode";

	protected SyncDirection SyncDirection { get; } = Direction;

	public async Task SyncAsync (CancellationToken token = default)
	{
		// use marille channel Hub mechanism for better async file creation
		// configuration.Mode = ChannelDeliveryMode.AtMostOnceAsync; // don't care about order of file writes..just need to get them written!
		await ConfigureMarilleHub ();

		if (SyncDirection == SyncDirection.ToXcode)
			await SyncToXcodeAsync (token).ConfigureAwait (false);
		else
			await SyncFromXcodeAsync (token).ConfigureAwait (false);

		Logger.Debug (Strings.SyncContext.SyncComplete);
	}

	async Task SyncToXcodeAsync (CancellationToken token)
	{
		Logger.Debug (Strings.SyncContext.GeneratingFiles);

		if (!xcSync.TryGetTargetPlatform (Logger, Framework, out string targetPlatform))
			return;

		var clrProject = new ClrProject (FileSystem, Logger!, TypeService, "CLR Project", ProjectPath, Framework);
		await clrProject.OpenProject ();

		HashSet<string> frameworks = ["Foundation", "Cocoa"];

		// match target platform to build settings id
		(string sdkroot, string deployment) = Framework switch {
			"macos" => ("macosx", "macosx"),
			"ios" => ("iphoneos", "iphoneos"),
			"maccatalyst" => ("iphoneos", "iphoneos"),
			"tvos" => ("appletvos", "tvos"),
			_ => ("", ""),
		};

		var projectName = FileSystem.Path.GetFileNameWithoutExtension (ProjectPath);

		// create in memory representation of Xcode assets
		var xcodeObjects = new Dictionary<string, XcodeObject> ();
		var pbxSourcesBuildFiles = new List<string> ();
		var pbxResourcesBuildFiles = new List<string> ();
		var pbxFrameworksBuildFiles = new List<string> ();
		var pbxGroupFiles = new List<string> ();

		var pbxFileReference = new PBXFileReference ();
		var pbxBuildFile = new PBXBuildFile ();

		var appFileReference = new PBXFileReference {
			Isa = "PBXFileReference",
			ExplicitFileType = "wrapper.application",
			Name = $"{projectName}.app",
			Path = $"{projectName}.app",
			SourceTree = "BUILT_PRODUCTS_DIR",
			IncludeInIndex = "0",
		};
		xcodeObjects.Add (appFileReference.Token, appFileReference);

		var productsGroup = new PBXGroup {
			Isa = "PBXGroup",
			Children = [appFileReference.Token],
			Name = "Products",
		};
		xcodeObjects.Add (productsGroup.Token, productsGroup);

		var pbxFrameworkFiles = new List<string> ();

		var frameworksGroup = new PBXGroup {
			Isa = "PBXGroup",
			Children = pbxFrameworkFiles,
			Name = "Frameworks",
		};
		xcodeObjects.Add (frameworksGroup.Token, frameworksGroup);

		var projectGroup = new PBXGroup {
			Isa = "PBXGroup",
			Children = pbxGroupFiles,
			Name = projectName,
		};
		xcodeObjects.Add (projectGroup.Token, projectGroup);

		var pbxGroup = new PBXGroup {
			Isa = "PBXGroup",
			Children = [
				productsGroup.Token,
				frameworksGroup.Token,
				projectGroup.Token
			]
		};
		xcodeObjects.Add (pbxGroup.Token, pbxGroup);

		foreach (var t in TypeService.QueryTypes ()) {
			if (t is null) continue;

			// all NSObjects get a corresponding .h + .m file generated
			// generated .h + .m files are added to the xcode project deps
			await GenerateAndWriteFile (".h", "sourcecode.c.h", () => new GenObjcH (t).TransformText (), TargetDir, t.ObjCType, xcodeObjects, pbxGroupFiles);
			var sourceFileRference = await GenerateAndWriteFile (".m", "sourcecode.c.objc", () => new GenObjcM (t).TransformText (), TargetDir, t.ObjCType, xcodeObjects, pbxGroupFiles);

			pbxBuildFile = new PBXBuildFile {
				Isa = "PBXBuildFile",
				FileRef = sourceFileRference.Token
			};

			xcodeObjects.Add (pbxBuildFile.Token, pbxBuildFile);
			pbxSourcesBuildFiles.Add (pbxBuildFile.Token);

			// add references for framework resolution at project level
			foreach (var r in t.References) {
				frameworks.Add (r);
			}
		}

		Logger?.Debug (Strings.Generate.GeneratedFiles);

		// leverage msbuild to get the list of files in the project
		var filePaths = Scripts.GetFiles (FileSystem, ProjectPath, Framework, targetPlatform);

		// copy storyboard, entitlements/info.plist files to the target directory 
		var appleFiles = filePaths.Where (path =>
					path.EndsWith (".plist", StringComparison.OrdinalIgnoreCase) ||
					path.EndsWith (".storyboard", StringComparison.OrdinalIgnoreCase) ||
					path.EndsWith (".xib", StringComparison.OrdinalIgnoreCase)
				).ToList ();

		// support for maui apps
		string altPath = targetPlatform switch {
			"ios" => FileSystem.Path.Combine (FileSystem.Path.GetDirectoryName (ProjectPath)!, "Platforms", "iOS"),
			"maccatalyst" => FileSystem.Path.Combine (FileSystem.Path.GetDirectoryName (ProjectPath)!, "Platforms", "MacCatalyst"),
			_ => ""
		};

		var appleDirectory = FileSystem.Path.Exists (altPath) ? altPath : FileSystem.Path.Combine (".", FileSystem.Path.GetDirectoryName (ProjectPath) ?? string.Empty);

		foreach (var file in appleFiles) {
			FileSystem.File.Copy (file, FileSystem.Path.Combine (TargetDir, FileSystem.Path.GetFileName (file)), true);

			// add to resources build phase
			pbxFileReference = new PBXFileReference {
				Isa = "PBXFileReference",
				LastKnownFileType = "text.plist.xml",
				Name = FileSystem.Path.GetFileName (file),
				Path = FileSystem.Path.GetFileName (file),
				SourceTree = "<group>"
			};
			xcodeObjects.Add (pbxFileReference.Token, pbxFileReference);
			pbxGroupFiles.Add (pbxFileReference.Token);

			pbxBuildFile = new PBXBuildFile {
				Isa = "PBXBuildFile",
				FileRef = pbxFileReference.Token
			};

			xcodeObjects.Add (pbxBuildFile.Token, pbxBuildFile);
			pbxResourcesBuildFiles.Add (pbxBuildFile.Token);
		}

		foreach (var f in frameworks) {
			string path = $"System/Library/Frameworks/{f}.framework";

			var fileReference = new PBXFileReference {
				Isa = "PBXFileReference",
				LastKnownFileType = "wrapper.framework",
				Name = FileSystem.Path.GetFileName (path),
				Path = path,
				SourceTree = "SDKROOT"
			};
			xcodeObjects.Add (fileReference.Token, fileReference);
			pbxFrameworkFiles.Add (fileReference.Token);

			var buildFile = new PBXBuildFile {
				Isa = "PBXBuildFile",
				FileRef = fileReference.Token
			};

			xcodeObjects.Add (buildFile.Token, buildFile);
			pbxFrameworksBuildFiles.Add (buildFile.Token);
		}

		// copy assets
		// single plat project support
		var assetsFolder = FileSystem.Directory
			.EnumerateDirectories (appleDirectory, "*.xcassets", SearchOption.TopDirectoryOnly).FirstOrDefault (); //TODO: add support for multiple asset folders
		if (assetsFolder is not null)
			Scripts.CopyDirectory (FileSystem, assetsFolder, FileSystem.Path.Combine (TargetDir, FileSystem.Path.GetFileName (assetsFolder)), true);

		// maui support
		foreach (var asset in Scripts.GetAssets (FileSystem, ProjectPath, Framework)) {
			Scripts.CopyDirectory (FileSystem, asset, FileSystem.Path.Combine (TargetDir, "Assets.xcassets"), true);

			pbxFileReference = new PBXFileReference {
				Isa = "PBXFileReference",
				LastKnownFileType = "folder.assetcatalog",
				Name = FileSystem.Path.GetFileName (asset),
				Path = FileSystem.Path.GetFileName (asset),
				SourceTree = "<group>"
			};
			xcodeObjects.Add (pbxFileReference.Token, pbxFileReference);
			pbxGroup.Children.Insert (0, pbxFileReference.Token);

			pbxBuildFile = new PBXBuildFile {
				Isa = "PBXBuildFile",
				FileRef = pbxFileReference.Token
			};

			xcodeObjects.Add (pbxBuildFile.Token, pbxBuildFile);
			pbxSourcesBuildFiles.Add (pbxBuildFile.Token);
		}

		var pbxResourcesBuildPhase = new PBXResourcesBuildPhase {
			Isa = "PBXResourcesBuildPhase",
			Files = pbxResourcesBuildFiles
		};
		xcodeObjects.Add (pbxResourcesBuildPhase.Token, pbxResourcesBuildPhase);

		var pbxSourcesBuildPhase = new PBXSourcesBuildPhase {
			Isa = "PBXSourcesBuildPhase",
			Files = pbxSourcesBuildFiles
		};
		xcodeObjects.Add (pbxSourcesBuildPhase.Token, pbxSourcesBuildPhase);

		var pbxFrameworksBuildPhase = new PBXFrameworksBuildPhase {
			Isa = "PBXFrameworksBuildPhase",
			Files = pbxFrameworksBuildFiles
		};
		xcodeObjects.Add (pbxFrameworksBuildPhase.Token, pbxFrameworksBuildPhase);

		var supportedOSVersion = Scripts.GetSupportedOSVersion (FileSystem, ProjectPath, Framework);

		var debugBuildConfiguration = new XCBuildConfiguration {
			Isa = "XCBuildConfiguration",
			BuildSettings = new Dictionary<string, IList<string>> {
				{"ALWAYS_SEARCH_USER_PATHS", new List<string> {"NO"}},
				{"CLANG_ANALYZER_NONNULL", new List<string> {"YES"}},
				{"CLANG_ANALYZER_NUMBER_OBJECT_CONVERSION", new List<string> {"YES_AGGRESSIVE"}},
				{"CLANG_CXX_LANGUAGE_STANDARD", new List<string> {"gnu++14"}},
				{"CLANG_CXX_LIBRARY", new List<string> {"libc++"}},
				{"CLANG_ENABLE_MODULES", new List<string> {"YES"}},
				{"CLANG_ENABLE_OBJC_ARC", new List<string> {"YES"}},
				{"CLANG_ENABLE_OBJC_WEAK", new List<string> {"YES"}},
				{"CLANG_WARN_BLOCK_CAPTURE_AUTORELEASING", new List<string> {"YES"}},
				{"CLANG_WARN_BOOL_CONVERSION", new List<string> {"YES"}},
				{"CLANG_WARN_COMMA", new List<string> {"YES"}},
				{"CLANG_WARN_CONSTANT_CONVERSION", new List<string> {"YES"}},
				{"CLANG_WARN_DEPRECATED_OBJC_IMPLEMENTATIONS", new List<string> {"YES"}},
				{"CLANG_WARN_DIRECT_OBJC_ISA_USAGE", new List<string> {"YES_ERROR"}},
				{"CLANG_WARN_DOCUMENTATION_COMMENTS", new List<string> {"YES"}},
				{"CLANG_WARN_EMPTY_BODY", new List<string> {"YES"}},
				{"CLANG_WARN_ENUM_CONVERSION", new List<string> {"YES"}},
				{"CLANG_WARN_INFINITE_RECURSION", new List<string> {"YES"}},
				{"CLANG_WARN_INT_CONVERSION", new List<string> {"YES"}},
				{"CLANG_WARN_NON_LITERAL_NULL_CONVERSION", new List<string> {"YES"}},
				{"CLANG_WARN_OBJC_IMPLICIT_RETAIN_SELF", new List<string> {"YES"}},
				{"CLANG_WARN_OBJC_LITERAL_CONVERSION", new List<string> {"YES"}},
				{"CLANG_WARN_OBJC_ROOT_CLASS", new List<string> {"YES_ERROR"}},
				{"CLANG_WARN_RANGE_LOOP_ANALYSIS", new List<string> {"YES"}},
				{"CLANG_WARN_STRICT_PROTOTYPES", new List<string> {"YES"}},
				{"CLANG_WARN_SUSPICIOUS_MOVE", new List<string> {"YES"}},
				{"CLANG_WARN_UNGUARDED_AVAILABILITY", new List<string> {"YES_AGGRESSIVE"}},
				{"CLANG_WARN_UNREACHABLE_CODE", new List<string> {"YES"}},
				{"COPY_PHASE_STRIP", new List<string> {"NO"}},
				{"DEBUG_INFORMATION_FORMAT", new List<string> {"dwarf"}},
				{"ENABLE_STRICT_OBJC_MSGSEND", new List<string> {"YES"}},
				{"ENABLE_TESTABILITY", new List<string> {"YES"}},
				{"GCC_C_LANGUAGE_STANDARD", new List<string> {"gnu11"}},
				{"GCC_DYNAMIC_NO_PIC", new List<string> {"NO"}},
				{"GCC_NO_COMMON_BLOCKS", new List<string> {"YES"}},
				{"GCC_OPTIMIZATION_LEVEL", new List<string> {"0"}},
				{"GCC_PREPROCESSOR_DEFINITIONS", new List<string> {"DEBUG=1","$(inherited)"}},
				{"GCC_WARN_64_TO_32_BIT_CONVERSION", new List<string> {"YES"}},
				{"GCC_WARN_ABOUT_RETURN_TYPE", new List<string> {"YES_ERROR"}},
				{"GCC_WARN_UNDECLARED_SELECTOR", new List<string> {"YES"}},
				{"GCC_WARN_UNINITIALIZED_AUTOS", new List<string> {"YES_AGGRESSIVE"}},
				{"GCC_WARN_UNUSED_FUNCTION", new List<string> {"YES"}},
				{"GCC_WARN_UNUSED_VARIABLE", new List<string> {"YES"}},
				{$"{deployment.ToUpper()}_DEPLOYMENT_TARGET", new List<string> {supportedOSVersion}},
				{"MTL_ENABLE_DEBUG_INFO", new List<string> {"YES"}},
				{"ONLY_ACTIVE_ARCH", new List<string> {"YES"}},
				{"SDKROOT", new List<string> {sdkroot}},
			},
			Name = "Debug",
		};
		xcodeObjects.Add (debugBuildConfiguration.Token, debugBuildConfiguration);

		var releaseBuildConfiguration = new XCBuildConfiguration {
			Isa = "XCBuildConfiguration",
			BuildSettings = new Dictionary<string, IList<string>> {
				{"ALWAYS_SEARCH_USER_PATHS", new List<string> {"NO"}},
				{"CLANG_ANALYZER_NONNULL", new List<string> {"YES"}},
				{"CLANG_ANALYZER_NUMBER_OBJECT_CONVERSION", new List<string> {"YES_AGGRESSIVE"}},
				{"CLANG_CXX_LANGUAGE_STANDARD", new List<string> {"gnu++14"}},
				{"CLANG_CXX_LIBRARY", new List<string> {"libc++"}},
				{"CLANG_ENABLE_MODULES", new List<string> {"YES"}},
				{"CLANG_ENABLE_OBJC_ARC", new List<string> {"YES"}},
				{"CLANG_ENABLE_OBJC_WEAK", new List<string> {"YES"}},
				{"CLANG_WARN_BLOCK_CAPTURE_AUTORELEASING", new List<string> {"YES"}},
				{"CLANG_WARN_BOOL_CONVERSION", new List<string> {"YES"}},
				{"CLANG_WARN_COMMA", new List<string> {"YES"}},
				{"CLANG_WARN_CONSTANT_CONVERSION", new List<string> {"YES"}},
				{"CLANG_WARN_DEPRECATED_OBJC_IMPLEMENTATIONS", new List<string> {"YES"}},
				{"CLANG_WARN_DIRECT_OBJC_ISA_USAGE", new List<string> {"YES_ERROR"}},
				{"CLANG_WARN_DOCUMENTATION_COMMENTS", new List<string> {"YES"}},
				{"CLANG_WARN_EMPTY_BODY", new List<string> {"YES"}},
				{"CLANG_WARN_ENUM_CONVERSION", new List<string> {"YES"}},
				{"CLANG_WARN_INFINITE_RECURSION", new List<string> {"YES"}},
				{"CLANG_WARN_INT_CONVERSION", new List<string> {"YES"}},
				{"CLANG_WARN_NON_LITERAL_NULL_CONVERSION", new List<string> {"YES"}},
				{"CLANG_WARN_OBJC_IMPLICIT_RETAIN_SELF", new List<string> {"YES"}},
				{"CLANG_WARN_OBJC_LITERAL_CONVERSION", new List<string> {"YES"}},
				{"CLANG_WARN_OBJC_ROOT_CLASS", new List<string> {"YES_ERROR"}},
				{"CLANG_WARN_RANGE_LOOP_ANALYSIS", new List<string> {"YES"}},
				{"CLANG_WARN_STRICT_PROTOTYPES", new List<string> {"YES"}},
				{"CLANG_WARN_SUSPICIOUS_MOVE", new List<string> {"YES"}},
				{"CLANG_WARN_UNGUARDED_AVAILABILITY", new List<string> {"YES_AGGRESSIVE"}},
				{"CLANG_WARN_UNREACHABLE_CODE", new List<string> {"YES"}},
				{"COPY_PHASE_STRIP", new List<string> {"NO"}},
				{"DEBUG_INFORMATION_FORMAT", new List<string> {"dwarf-with-dsym"}},
				{"ENABLE_NS_ASSERTIONS", new List<string> () {"NO"}},
				{"ENABLE_STRICT_OBJC_MSGSEND", new List<string> {"YES"}},
				{"GCC_C_LANGUAGE_STANDARD", new List<string> {"gnu11"}},
				{"GCC_NO_COMMON_BLOCKS", new List<string> {"YES"}},
				{"GCC_WARN_64_TO_32_BIT_CONVERSION", new List<string> {"YES"}},
				{"GCC_WARN_ABOUT_RETURN_TYPE", new List<string> {"YES_ERROR"}},
				{"GCC_WARN_UNDECLARED_SELECTOR", new List<string> {"YES"}},
				{"GCC_WARN_UNINITIALIZED_AUTOS", new List<string> {"YES_AGGRESSIVE"}},
				{"GCC_WARN_UNUSED_FUNCTION", new List<string> {"YES"}},
				{"GCC_WARN_UNUSED_VARIABLE", new List<string> {"YES"}},
				{$"{deployment.ToUpper()}_DEPLOYMENT_TARGET", new List<string> {supportedOSVersion}},
				{"MTL_ENABLE_DEBUG_INFO", new List<string> {"NO"}},
				{"SDKROOT", new List<string> {sdkroot}},
				{"VALIDATE_PRODUCT", new List<string> {"YES"}},
			},
			Name = "Release",
		};
		xcodeObjects.Add (releaseBuildConfiguration.Token, releaseBuildConfiguration);

		var commonTargetBuildSettings = new Dictionary<string, IList<string>> {
			["INFOPLIST_FILE"] = ["Info.plist"],
			["PRODUCT_NAME"] = ["$(TARGET_NAME)"],
			["ASSETCATALOG_COMPILER_APPICON_NAME"] = ["AppIcon"],
			["CODE_SIGN_STYLE"] = ["Automatic"],
			["LD_RUNPATH_SEARCH_PATHS"] = ["$(inherited)", "@executable_path/../Frameworks"],
			["PRODUCT_BUNDLE_IDENTIFIER"] = [$"com.companyname.{projectName}"],
		};

		var debugTargetBuildConfiguration = new XCBuildConfiguration {
			Isa = "XCBuildConfiguration",
			BuildSettings = commonTargetBuildSettings,
			Name = "Debug",
		};
		xcodeObjects.Add (debugTargetBuildConfiguration.Token, debugTargetBuildConfiguration);

		var releaseTargetBuildConfiguration = new XCBuildConfiguration {
			Isa = "XCBuildConfiguration",
			BuildSettings = commonTargetBuildSettings,
			Name = "Release",
		};
		xcodeObjects.Add (releaseTargetBuildConfiguration.Token, releaseTargetBuildConfiguration);

		var targetBuildCombo = new XCConfigurationList {
			Isa = "XCConfigurationList",
			BuildConfigurations = [
				debugTargetBuildConfiguration.Token,
				releaseTargetBuildConfiguration.Token],
			DefaultConfigurationName = "Debug",
			DefaultConfigurationIsVisible = "0",
		};
		xcodeObjects.Add (targetBuildCombo.Token, targetBuildCombo);

		var buildCombo = new XCConfigurationList {
			Isa = "XCConfigurationList",
			BuildConfigurations = [
				debugBuildConfiguration.Token,
				releaseBuildConfiguration.Token],
			DefaultConfigurationName = "Debug",
			DefaultConfigurationIsVisible = "0",
		};
		xcodeObjects.Add (buildCombo.Token, buildCombo);

		var nativeTarget = new PBXNativeTarget {
			Isa = "PBXNativeTarget",
			BuildConfigurationList = targetBuildCombo.Token,
			BuildPhases = [pbxSourcesBuildPhase.Token, pbxResourcesBuildPhase.Token, pbxFrameworksBuildPhase.Token],
			BuildRules = [],
			Dependencies = [],
			Name = projectName,
			ProductName = projectName,
			ProductReference = appFileReference.Token,
			ProductType = "com.apple.product-type.application"
		};
		xcodeObjects.Add (nativeTarget.Token, nativeTarget);

		var pbxProject = new PBXProject {
			Isa = "PBXProject",
			Attributes = new Dictionary<string, string> {
				{"LastUpgradeCheck", "0930"}
			},
			BuildConfigurationList = buildCombo.Token,
			CompatibilityVersion = "Xcode 9.3",
			DevelopmentRegion = "en",
			HasScannedForEncodings = "0",
			KnownRegions = ["en"],
			MainGroup = pbxGroup.Token,
			ProductRefGroup = productsGroup.Token,
			ProjectDirPath = "",
			ProjectRoot = "",
			Targets = [nativeTarget.Token]
		};
		xcodeObjects.Add (pbxProject.Token, pbxProject);

		var xcodeProject = new XcodeProject {
			ArchiveVersion = "1",
			ObjectVersion = "50",
			RootObject = pbxProject.Token,
			Objects = xcodeObjects,
		};

		// generate xcode workspace
		XcodeWorkspaceGenerator.Generate (FileSystem, projectName, Environment.UserName, TargetDir, xcodeProject);
		Logger?.Information (Strings.Generate.GeneratedProject (TargetDir));

		if (Open) {
			string workspacePath = FileSystem.Path.GetFullPath (FileSystem.Path.Combine (TargetDir, projectName + ".xcodeproj", "project.xcworkspace"));
			Logger?.Information (Strings.Generate.OpenProject (Scripts.Run (Scripts.OpenXcodeProject (workspacePath))));
		}
		return;
	}

	async Task SyncFromXcodeAsync (CancellationToken token)
	{
		List<Task> jobs = [];

		Logger.Information (Strings.Sync.HeaderInformation, TargetDir, ProjectPath);

		var projectName = FileSystem.Path.GetFileNameWithoutExtension (ProjectPath);

		var dotNetProject = new ClrProject (FileSystem, Logger, TypeService, projectName, ProjectPath, Framework);
		await dotNetProject.OpenProject ().ConfigureAwait (false);

		var xcodeWorkspace = new XcodeWorkspace (FileSystem, Logger, TypeService, projectName, TargetDir, Framework);

		await xcodeWorkspace.LoadAsync (token).ConfigureAwait (false);

		var typeLoader = new ObjCTypesLoader (Logger);
		ObjCTypeLoaderErrorWorker errorWorker = new ();
		await Hub.CreateAsync<LoadTypesFromObjCMessage> (SyncChannel, configuration, errorWorker);
		await Hub.RegisterAsync (SyncChannel, typeLoader);
		foreach (var syncItem in xcodeWorkspace.Items) {
			jobs.Add (syncItem switch {
				SyncableType type => /* Hub.Publish (SyncChannel, new LoadTypesFromObjCMessage (Guid.NewGuid ().ToString (), xcodeWorkspace, syncItem)) */
										typeLoader.ConsumeAsync (new LoadTypesFromObjCMessage (Guid.NewGuid ().ToString (), xcodeWorkspace, syncItem), token),
				_ => Task.CompletedTask
			});
		}
		Task.WaitAll ([.. jobs], token);
		jobs.Clear ();

		var typesToWrite = TypeService.QueryTypes (null, null) // All Types
			.Where (t => t is not null && t.InDesigner) ?? []; // Filter Types that are in .designer.cs files TODO: This may be wrong, there are types that don't exist in *.designer.cs files

		// TODO: What happens when a new type is added to the Xcode project, like new view controllers?
		var fileWorker = new FileWorker (Logger, FileSystem);
		FileErrorWorker fileErrorWorker = new ();
		await Hub.CreateAsync<FileMessage> (FileChannel, configuration, fileErrorWorker);
		await Hub.RegisterAsync (FileChannel, fileWorker);

		foreach (var type in typesToWrite) {
			Logger.Information ("Processing type {Type}", type?.ClrType);

			// This gets the SyntaxTree from the *.designer.cs portion of the type
			var typeSymbol = type?.TypeSymbol!;
			SyntaxTree? syntaxTree = null;
			foreach (var syntaxRef in typeSymbol.DeclaringSyntaxReferences) {
				if (syntaxRef.SyntaxTree.FilePath.EndsWith ("designer.cs")) {
					syntaxTree = syntaxRef.SyntaxTree;
					break;
				}
			}

			// Write out the file
			// TODO: This should probably be extracted to a different class, like SyncableFile/Type
			//await WriteFile (syntaxTree!.FilePath, syntaxTree?.GetRoot (token).GetText ().ToString () ?? string.Empty);

			jobs.Add (fileWorker.ConsumeAsync (new FileMessage {
				Id = Guid.NewGuid ().ToString (),
				Path = syntaxTree!.FilePath,
				Content = syntaxTree?.GetRoot (token).GetText ().ToString () ?? string.Empty
			}, token));
		}
		Task.WaitAll ([.. jobs], token);
		jobs.Clear ();
	}

	protected async override Task ConfigureMarilleHub ()
	{
		await base.ConfigureMarilleHub ();
		var fileWorker = new FileWorker (Logger, FileSystem);
		FileErrorWorker fileErrorWorker = new ();
		await Hub.CreateAsync<FileMessage> (FileChannel, configuration, fileErrorWorker);
		await Hub.RegisterAsync (FileChannel, fileWorker);
		var otlWorker = new ObjCTypesLoader (Logger);
		ObjCTypeLoaderErrorWorker errorWorker = new ();
		await Hub.CreateAsync<LoadTypesFromObjCMessage> (SyncChannel, configuration, errorWorker);
		await Hub.RegisterAsync (SyncChannel, otlWorker);
	}

	public async Task WriteFile (string path, string content) =>
		await Hub.PublishAsync (FileChannel, new FileMessage {
			Id = Guid.NewGuid ().ToString (),
			Path = path,
			Content = content
		});

	async Task<PBXFileReference> GenerateAndWriteFile (string extension, string fileType, Func<string> generateContent, string targetDir, string objcType, Dictionary<string, XcodeObject> xcodeObjects, List<string> pbxGroupFiles)
	{
		var content = generateContent ();
		var filePath = FileSystem.Path.Combine (targetDir, objcType + extension);
		await WriteFile (filePath, content);
		var pbxFileReference = new PBXFileReference {
			Isa = "PBXFileReference",
			LastKnownFileType = fileType,
			Name = FileSystem.Path.GetFileName (filePath),
			Path = FileSystem.Path.GetFileName (filePath),
			SourceTree = "<group>"
		};
		xcodeObjects.Add (pbxFileReference.Token, pbxFileReference);
		pbxGroupFiles.Add (pbxFileReference.Token);
		return pbxFileReference;
	}
}
