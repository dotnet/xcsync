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

class SyncContext (IFileSystem fileSystem, ITypeService typeService, SyncDirection Direction, string projectPath, string targetDir, string framework, ILogger logger, bool Open = false, bool force = false)
	: SyncContextBase (fileSystem, typeService, projectPath, targetDir, framework, logger) {

	public const string FileChannel = "Files";
	public const string SyncChannel = "SyncFromXcode";

	public const string YES = nameof (YES);
	public const string NO = nameof (NO);

	protected SyncDirection SyncDirection { get; } = Direction;
	protected string ProjectDir => FileSystem.Path.GetDirectoryName (FileSystem.Path.GetFullPath (ProjectPath))!;

	public async Task SyncAsync (CancellationToken token = default)
	{
		await ConfigureMarilleHub ();

		if (SyncDirection == SyncDirection.ToXcode)
			await SyncToXcodeAsync (token).ConfigureAwait (false);
		else
			await SyncFromXcodeAsync (token).ConfigureAwait (false);

		await Hub.CloseAllAsync ();

		Logger.Debug (Strings.SyncContext.SyncComplete);
	}

	async Task SyncToXcodeAsync (CancellationToken token)
	{
		Logger.Debug (Strings.SyncContext.GeneratingFiles);

		if (!xcSync.TryGetTargetPlatform (Logger, Framework.Platform, out string targetPlatform))
			return;

		var clrProject = new ClrProject (FileSystem, Logger!, TypeService, "CLR", ProjectPath, Framework.ToString ());
		await clrProject.OpenProject ();

		var projectName = FileSystem.Path.GetFileNameWithoutExtension (ProjectPath);
		string projectFilesPath = FileSystem.Path.GetFullPath (FileSystem.Path.Combine (TargetDir, projectName));

		if (force && FileSystem.Directory.Exists (projectFilesPath)) {
			// remove existing xcode project
			FileSystem.Directory.Delete (projectFilesPath, true);
		}

		if (!FileSystem.Directory.Exists (TargetDir)) {
			FileSystem.Directory.CreateDirectory (TargetDir);
		}

		HashSet<string> frameworks = ["Foundation", "Cocoa"];

		// match target platform to build settings id
		(string sdkroot, string deployment) = Framework.Platform switch {
			"macos" => ("macosx", "macosx"),
			"ios" => ("iphoneos", "iphoneos"),
			"maccatalyst" => ("iphoneos", "iphoneos"),
			"tvos" => ("appletvos", "tvos"),
			_ => ("", ""),
		};


		// create in memory representation of Xcode assets
		var xcodeObjects = new Dictionary<string, XcodeObject> ();
		var pbxSourcesBuildFiles = new List<string> ();
		var pbxResourcesBuildFiles = new List<string> ();
		var pbxFrameworksBuildFiles = new List<string> ();
		var pbxGroupFiles = new List<string> ();

		var pbxFileReference = new PBXFileReference ();
		var pbxBuildFile = new PBXBuildFile ();

		var appFileReference = new PBXFileReference {
			Isa = nameof (PBXFileReference),
			ExplicitFileType = "wrapper.application",
			Name = $"{projectName}.app",
			Path = $"{projectName}.app",
			SourceTree = "BUILT_PRODUCTS_DIR",
			IncludeInIndex = "0",
		};
		xcodeObjects.Add (appFileReference.Token, appFileReference);

		var productsGroup = new PBXGroup {
			Isa = nameof (PBXGroup),
			Children = [appFileReference.Token],
			Name = "Products",
		};
		xcodeObjects.Add (productsGroup.Token, productsGroup);

		var pbxFrameworkFiles = new List<string> ();

		var frameworksGroup = new PBXGroup {
			Isa = nameof (PBXGroup),
			Children = pbxFrameworkFiles,
			Name = "Frameworks",
		};
		xcodeObjects.Add (frameworksGroup.Token, frameworksGroup);

		var projectGroup = new PBXGroup {
			Isa = nameof (PBXGroup),
			Children = pbxGroupFiles,
			Name = projectName,
		};
		xcodeObjects.Add (projectGroup.Token, projectGroup);

		var pbxGroup = new PBXGroup {
			Isa = nameof (PBXGroup),
			Children = [
				productsGroup.Token,
				frameworksGroup.Token,
				projectGroup.Token
			]
		};
		xcodeObjects.Add (pbxGroup.Token, pbxGroup);

		foreach (var t in TypeService.QueryTypes ().Where (t => t is not null && t.IsInSource)) {

			if (t is null) continue; // Only needed to keep the compiler happy

			// all NSObjects get a corresponding .h + .m file generated
			// generated .h + .m files are added to the xcode project deps
			await GenerateAndWriteFile (".h", "sourcecode.c.h", () => new GenObjcH (t).TransformText (), TargetDir, t.ObjCType, xcodeObjects, pbxGroupFiles);
			var sourceFileRference = await GenerateAndWriteFile (".m", "sourcecode.c.objc", () => new GenObjcM (t).TransformText (), TargetDir, t.ObjCType, xcodeObjects, pbxGroupFiles);

			pbxBuildFile = new PBXBuildFile {
				Isa = nameof (PBXBuildFile),
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
		var filePaths = Scripts.GetFileItemsFromProject (ProjectPath, Framework.Platform, targetPlatform);

		// copy storyboard, entitlements/info.plist files to the target directory 
		var appleFiles = filePaths.Where (path =>
					path.EndsWith (".plist", StringComparison.OrdinalIgnoreCase) ||
					path.EndsWith (".storyboard", StringComparison.OrdinalIgnoreCase) ||
					path.EndsWith (".xib", StringComparison.OrdinalIgnoreCase)
				).ToList ();

		// support for maui apps
		string altPath = targetPlatform switch {
			"ios" => FileSystem.Path.Combine (ProjectDir, "Platforms", "iOS"),
			"maccatalyst" => FileSystem.Path.Combine (ProjectDir, "Platforms", "MacCatalyst"),
			_ => ""
		};

		var appleDirectory = FileSystem.Path.Exists (altPath) ? altPath : FileSystem.Path.Combine (".", ProjectDir ?? string.Empty);

		foreach (var file in appleFiles) {
			// get path relative to project, and then copy to target directory to ensure relative paths are maintained during sync
			var relativePath = FileSystem.Path.GetRelativePath (ProjectDir!, file);
			var relativeParentPath = FileSystem.Path.GetDirectoryName (relativePath);
			if (relativeParentPath is not null)
				FileSystem.Directory.CreateDirectory (FileSystem.Path.Combine (TargetDir, relativeParentPath));
			await Hub.PublishAsync (FileChannel, new CopyFileMessage (Guid.NewGuid ().ToString (), file, FileSystem.Path.Combine (TargetDir, relativePath)));

			// add to resources build phase
			pbxFileReference = new PBXFileReference {
				Isa = nameof (PBXFileReference),
				LastKnownFileType = "text.plist.xml",
				Name = FileSystem.Path.GetFileName (file),
				Path = relativePath,
				SourceTree = "<group>"
			};
			xcodeObjects.Add (pbxFileReference.Token, pbxFileReference);
			pbxGroupFiles.Add (pbxFileReference.Token);

			pbxBuildFile = new PBXBuildFile {
				Isa = nameof (PBXBuildFile),
				FileRef = pbxFileReference.Token
			};

			xcodeObjects.Add (pbxBuildFile.Token, pbxBuildFile);
			pbxResourcesBuildFiles.Add (pbxBuildFile.Token);
		}

		foreach (var f in frameworks) {
			string path = $"System/Library/Frameworks/{f}.framework";

			var fileReference = new PBXFileReference {
				Isa = nameof (PBXFileReference),
				LastKnownFileType = "wrapper.framework",
				Name = FileSystem.Path.GetFileName (path),
				Path = path,
				SourceTree = "SDKROOT"
			};
			xcodeObjects.Add (fileReference.Token, fileReference);
			pbxFrameworkFiles.Add (fileReference.Token);

			var buildFile = new PBXBuildFile {
				Isa = nameof (PBXBuildFile),
				FileRef = fileReference.Token
			};

			xcodeObjects.Add (buildFile.Token, buildFile);
			pbxFrameworksBuildFiles.Add (buildFile.Token);
		}

		// copy assets
		foreach (var asset in Scripts.GetAssetItemsFromProject (ProjectPath, Framework.ToString ())) {
			await Hub.PublishAsync (FileChannel, new CopyFileMessage (Guid.NewGuid ().ToString (), asset, FileSystem.Path.Combine (TargetDir, "Assets.xcassets")));
			AddAsset (asset);
		}

		void AddAsset (string asset)
		{
			pbxFileReference = new PBXFileReference {
				Isa = nameof (PBXFileReference),
				LastKnownFileType = "folder.assetcatalog",
				Name = FileSystem.Path.GetFileName (asset),
				Path = FileSystem.Path.GetFileName (asset),
				SourceTree = "<group>"
			};
			xcodeObjects.Add (pbxFileReference.Token, pbxFileReference);
			pbxGroup.Children.Insert (0, pbxFileReference.Token);

			pbxBuildFile = new PBXBuildFile {
				Isa = nameof (PBXBuildFile),
				FileRef = pbxFileReference.Token
			};

			xcodeObjects.Add (pbxBuildFile.Token, pbxBuildFile);
			pbxResourcesBuildFiles.Add (pbxBuildFile.Token);
		}

		var pbxResourcesBuildPhase = new PBXResourcesBuildPhase {
			Isa = nameof (PBXResourcesBuildPhase),
			Files = pbxResourcesBuildFiles
		};
		xcodeObjects.Add (pbxResourcesBuildPhase.Token, pbxResourcesBuildPhase);

		var pbxSourcesBuildPhase = new PBXSourcesBuildPhase {
			Isa = nameof (PBXSourcesBuildPhase),
			Files = pbxSourcesBuildFiles
		};
		xcodeObjects.Add (pbxSourcesBuildPhase.Token, pbxSourcesBuildPhase);

		var pbxFrameworksBuildPhase = new PBXFrameworksBuildPhase {
			Isa = nameof (PBXFrameworksBuildPhase),
			Files = pbxFrameworksBuildFiles
		};
		xcodeObjects.Add (pbxFrameworksBuildPhase.Token, pbxFrameworksBuildPhase);

		var supportedOSVersion = Scripts.GetSupportedOSVersionForTfmFromProject (ProjectPath, Framework.ToString ());

		var debugBuildConfiguration = new XCBuildConfiguration {
			Isa = nameof (XCBuildConfiguration),
			BuildSettings = new Dictionary<string, IList<string>> {
				{"ALWAYS_SEARCH_USER_PATHS", [NO]},
				{"CLANG_ANALYZER_NONNULL", [YES]},
				{"CLANG_ANALYZER_NUMBER_OBJECT_CONVERSION", ["YES_AGGRESSIVE"]},
				{"CLANG_CXX_LANGUAGE_STANDARD", ["gnu++14"]},
				{"CLANG_CXX_LIBRARY", ["libc++"]},
				{"CLANG_ENABLE_MODULES", [YES]},
				{"CLANG_ENABLE_OBJC_ARC", [YES]},
				{"CLANG_ENABLE_OBJC_WEAK", [YES]},
				{"CLANG_WARN_BLOCK_CAPTURE_AUTORELEASING", [YES]},
				{"CLANG_WARN_BOOL_CONVERSION", [YES]},
				{"CLANG_WARN_COMMA", [YES]},
				{"CLANG_WARN_CONSTANT_CONVERSION", [YES]},
				{"CLANG_WARN_DEPRECATED_OBJC_IMPLEMENTATIONS", [YES]},
				{"CLANG_WARN_DIRECT_OBJC_ISA_USAGE", ["YES_ERROR"]},
				{"CLANG_WARN_DOCUMENTATION_COMMENTS", [YES]},
				{"CLANG_WARN_EMPTY_BODY", [YES]},
				{"CLANG_WARN_ENUM_CONVERSION", [YES]},
				{"CLANG_WARN_INFINITE_RECURSION", [YES]},
				{"CLANG_WARN_INT_CONVERSION", [YES]},
				{"CLANG_WARN_NON_LITERAL_NULL_CONVERSION", [YES]},
				{"CLANG_WARN_OBJC_IMPLICIT_RETAIN_SELF", [YES]},
				{"CLANG_WARN_OBJC_LITERAL_CONVERSION", [YES]},
				{"CLANG_WARN_OBJC_ROOT_CLASS", ["YES_ERROR"]},
				{"CLANG_WARN_RANGE_LOOP_ANALYSIS", [YES]},
				{"CLANG_WARN_STRICT_PROTOTYPES", [YES]},
				{"CLANG_WARN_SUSPICIOUS_MOVE", [YES]},
				{"CLANG_WARN_UNGUARDED_AVAILABILITY", ["YES_AGGRESSIVE"]},
				{"CLANG_WARN_UNREACHABLE_CODE", [YES]},
				{"COPY_PHASE_STRIP", [NO]},
				{"DEBUG_INFORMATION_FORMAT", ["dwarf"]},
				{"ENABLE_STRICT_OBJC_MSGSEND", [YES]},
				{"ENABLE_TESTABILITY", [YES]},
				{"GCC_C_LANGUAGE_STANDARD", ["gnu11"]},
				{"GCC_DYNAMIC_NO_PIC", [NO]},
				{"GCC_NO_COMMON_BLOCKS", [YES]},
				{"GCC_OPTIMIZATION_LEVEL", ["0"]},
				{"GCC_PREPROCESSOR_DEFINITIONS", ["DEBUG=1","$(inherited)"]},
				{"GCC_WARN_64_TO_32_BIT_CONVERSION", [YES]},
				{"GCC_WARN_ABOUT_RETURN_TYPE", ["YES_ERROR"]},
				{"GCC_WARN_UNDECLARED_SELECTOR", [YES]},
				{"GCC_WARN_UNINITIALIZED_AUTOS", ["YES_AGGRESSIVE"]},
				{"GCC_WARN_UNUSED_FUNCTION", [YES]},
				{"GCC_WARN_UNUSED_VARIABLE", [YES]},
				{$"{deployment.ToUpper()}_DEPLOYMENT_TARGET", [supportedOSVersion]},
				{"MTL_ENABLE_DEBUG_INFO", [YES]},
				{"ONLY_ACTIVE_ARCH", [YES]},
				{"SDKROOT", [sdkroot]},
			},
			Name = "Debug",
		};
		xcodeObjects.Add (debugBuildConfiguration.Token, debugBuildConfiguration);

		var releaseBuildConfiguration = new XCBuildConfiguration {
			Isa = nameof (XCBuildConfiguration),
			BuildSettings = new Dictionary<string, IList<string>> {
				{"ALWAYS_SEARCH_USER_PATHS", [NO]},
				{"CLANG_ANALYZER_NONNULL", [YES]},
				{"CLANG_ANALYZER_NUMBER_OBJECT_CONVERSION", ["YES_AGGRESSIVE"]},
				{"CLANG_CXX_LANGUAGE_STANDARD", ["gnu++14"]},
				{"CLANG_CXX_LIBRARY", ["libc++"]},
				{"CLANG_ENABLE_MODULES", [YES]},
				{"CLANG_ENABLE_OBJC_ARC", [YES]},
				{"CLANG_ENABLE_OBJC_WEAK", [YES]},
				{"CLANG_WARN_BLOCK_CAPTURE_AUTORELEASING", [YES]},
				{"CLANG_WARN_BOOL_CONVERSION", [YES]},
				{"CLANG_WARN_COMMA", [YES]},
				{"CLANG_WARN_CONSTANT_CONVERSION", [YES]},
				{"CLANG_WARN_DEPRECATED_OBJC_IMPLEMENTATIONS", [YES]},
				{"CLANG_WARN_DIRECT_OBJC_ISA_USAGE", ["YES_ERROR"]},
				{"CLANG_WARN_DOCUMENTATION_COMMENTS", [YES]},
				{"CLANG_WARN_EMPTY_BODY", [YES]},
				{"CLANG_WARN_ENUM_CONVERSION", [YES]},
				{"CLANG_WARN_INFINITE_RECURSION", [YES]},
				{"CLANG_WARN_INT_CONVERSION", [YES]},
				{"CLANG_WARN_NON_LITERAL_NULL_CONVERSION", [YES]},
				{"CLANG_WARN_OBJC_IMPLICIT_RETAIN_SELF", [YES]},
				{"CLANG_WARN_OBJC_LITERAL_CONVERSION", [YES]},
				{"CLANG_WARN_OBJC_ROOT_CLASS", ["YES_ERROR"]},
				{"CLANG_WARN_RANGE_LOOP_ANALYSIS", [YES]},
				{"CLANG_WARN_STRICT_PROTOTYPES", [YES]},
				{"CLANG_WARN_SUSPICIOUS_MOVE", [YES]},
				{"CLANG_WARN_UNGUARDED_AVAILABILITY", ["YES_AGGRESSIVE"]},
				{"CLANG_WARN_UNREACHABLE_CODE", [YES]},
				{"COPY_PHASE_STRIP", [NO]},
				{"DEBUG_INFORMATION_FORMAT", ["dwarf-with-dsym"]},
				{"ENABLE_NS_ASSERTIONS", [NO]},
				{"ENABLE_STRICT_OBJC_MSGSEND", [YES]},
				{"GCC_C_LANGUAGE_STANDARD", ["gnu11"]},
				{"GCC_NO_COMMON_BLOCKS", [YES]},
				{"GCC_WARN_64_TO_32_BIT_CONVERSION", [YES]},
				{"GCC_WARN_ABOUT_RETURN_TYPE", ["YES_ERROR"]},
				{"GCC_WARN_UNDECLARED_SELECTOR", [YES]},
				{"GCC_WARN_UNINITIALIZED_AUTOS", ["YES_AGGRESSIVE"]},
				{"GCC_WARN_UNUSED_FUNCTION", [YES]},
				{"GCC_WARN_UNUSED_VARIABLE", [YES]},
				{$"{deployment.ToUpper()}_DEPLOYMENT_TARGET", [supportedOSVersion]},
				{"MTL_ENABLE_DEBUG_INFO", [NO]},
				{"SDKROOT", [sdkroot]},
				{"VALIDATE_PRODUCT", [YES]},
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
			Isa = nameof (XCBuildConfiguration),
			BuildSettings = commonTargetBuildSettings,
			Name = "Debug",
		};
		xcodeObjects.Add (debugTargetBuildConfiguration.Token, debugTargetBuildConfiguration);

		var releaseTargetBuildConfiguration = new XCBuildConfiguration {
			Isa = nameof (XCBuildConfiguration),
			BuildSettings = commonTargetBuildSettings,
			Name = "Release",
		};
		xcodeObjects.Add (releaseTargetBuildConfiguration.Token, releaseTargetBuildConfiguration);

		var targetBuildCombo = new XCConfigurationList {
			Isa = nameof (XCConfigurationList),
			BuildConfigurations = [
				debugTargetBuildConfiguration.Token,
				releaseTargetBuildConfiguration.Token],
			DefaultConfigurationName = "Debug",
			DefaultConfigurationIsVisible = "0",
		};
		xcodeObjects.Add (targetBuildCombo.Token, targetBuildCombo);

		var buildCombo = new XCConfigurationList {
			Isa = nameof (XCConfigurationList),
			BuildConfigurations = [
				debugBuildConfiguration.Token,
				releaseBuildConfiguration.Token],
			DefaultConfigurationName = "Debug",
			DefaultConfigurationIsVisible = "0",
		};
		xcodeObjects.Add (buildCombo.Token, buildCombo);

		var nativeTarget = new PBXNativeTarget {
			Isa = nameof (PBXNativeTarget),
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
			Isa = nameof (PBXProject),
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
			Logger?.Information (Strings.Generate.OpenProject (Scripts.RunAppleScript (Scripts.OpenXcodeProject (workspacePath))));
		}
		return;
	}

	async Task SyncFromXcodeAsync (CancellationToken token)
	{
		Logger.Information (Strings.Sync.HeaderInformation, TargetDir, ProjectPath);

		var projectName = FileSystem.Path.GetFileNameWithoutExtension (ProjectPath);

		var dotNetProject = new ClrProject (FileSystem, Logger, TypeService, projectName, ProjectPath, Framework.ToString ());
		await dotNetProject.OpenProject ().ConfigureAwait (false);

		var xcodeWorkspace = new XcodeWorkspace (FileSystem, Logger, TypeService, projectName, TargetDir, Framework.ToString ());

		var xcodeproj = FileSystem.Path.Combine (xcodeWorkspace.RootPath, $"{projectName}.xcodeproj");
		var pbxProjPath = FileSystem.Path.Combine (xcodeproj, "project.pbxproj");
		if (!FileSystem.File.Exists (pbxProjPath)) {
			Logger.Fatal (Strings.Errors.PbxprojNotFound (xcodeproj));
			return;
		}
		Scripts.ConvertPbxProjToJson (pbxProjPath);

		await xcodeWorkspace.LoadAsync (token).ConfigureAwait (false);

		var platformFolder = string.Empty;

		if (dotNetProject.IsMauiApp) {
			platformFolder = Framework.Platform switch {
				"ios" => FileSystem.Path.Combine ("Platforms", "iOS"),
				"maccatalyst" => FileSystem.Path.Combine ("Platforms", "MacCatalyst"),
				_ => "."
			};
		}

		List<Task> tasks = [];
		foreach (var syncItem in xcodeWorkspace.Items) {
			var basePath = string.Empty;
			if (syncItem is SyncableContent content && (
				content.SourcePath.EndsWith (".xcassets", StringComparison.OrdinalIgnoreCase) ||
				content.SourcePath.EndsWith (".plist", StringComparison.OrdinalIgnoreCase)
			)) {
				basePath = platformFolder;
			}
			TaskCompletionSource? tcs = null;
			await (syncItem switch {
#pragma warning disable CA2012 // Use ValueTasks correctly  
				SyncableType type => Hub.PublishAsync (SyncChannel, new LoadTypesFromObjCMessage (Guid.NewGuid ().ToString (), tcs = new TaskCompletionSource (), xcodeWorkspace, syncItem)),
				SyncableContent file when string.IsNullOrEmpty (basePath) => Hub.PublishAsync (FileChannel, new CopyFileMessage (Guid.NewGuid ().ToString (), file.SourcePath, FileSystem.Path.Combine (ProjectDir!, FileSystem.Path.Combine (basePath, file.DestinationPath)))),
				SyncableContent file => Hub.PublishAsync (FileChannel, new CopyFileMessage (Guid.NewGuid ().ToString (), file.SourcePath, FileSystem.Path.Combine (ProjectDir!, FileSystem.Path.Combine (basePath, FileSystem.Path.GetRelativePath (basePath, file.DestinationPath))))),
				_ => ValueTask.CompletedTask
#pragma warning restore CA2012 // Use ValueTasks correctly
			}).ConfigureAwait (false);
			if (tcs is not null) {
				tasks.Add (tcs.Task);
			}
		}
		Task.WaitAll ([.. tasks], token);

		var typesToWrite = TypeService.QueryTypes (null, null) // All Types
			.Where (t => t is not null && t.InDesigner) ?? []; // Filter Types that are in .designer.cs files TODO: This may be wrong, there are types that don't exist in *.designer.cs files

		// TODO: What happens when a new type is added to the Xcode project, like new view controllers?

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
			await WriteFile (syntaxTree!.FilePath, syntaxTree?.GetRoot (token).GetText ().ToString () ?? string.Empty);
		}
	}

	protected async override Task ConfigureMarilleHub ()
	{
		await base.ConfigureMarilleHub ();

		var fileWorker = new FileWorker (Logger, FileSystem);
		await Hub.CreateAsync (FileChannel, configuration, fileWorker);
		await Hub.RegisterAsync (FileChannel, fileWorker);

		var copyWorker = new CopyFileWorker (Logger, FileSystem);
		await Hub.CreateAsync (FileChannel, configuration, copyWorker);
		await Hub.RegisterAsync (FileChannel, copyWorker);

		var otlWorker = new ObjCTypesLoader (Logger);
		await Hub.CreateAsync (SyncChannel, configuration, otlWorker);
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
			Isa = nameof (PBXFileReference),
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
