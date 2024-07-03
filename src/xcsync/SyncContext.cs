// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO.Abstractions;
using Serilog;
using xcsync.Projects;
using xcsync.Workers;
using xcsync.Projects.Xcode;
using Marille;
using Microsoft.CodeAnalysis;

namespace xcsync;

class SyncContext (IFileSystem fileSystem, ITypeService typeService, SyncDirection Direction, string projectPath, string targetDir, string framework, ILogger logger, bool Open = false)
	: SyncContextBase (fileSystem, typeService, projectPath, targetDir, framework, logger) {

	public const string FileChannel = "Files";

	protected SyncDirection SyncDirection { get; } = Direction;

	public async Task SyncAsync (CancellationToken token = default)
	{
		// use marille channel hub mechanism for better async file creation
		configuration.Mode = ChannelDeliveryMode.AtMostOnceAsync; // don't care about order of file writes..just need to get them written!
		await Hub.CreateAsync<FileMessage> (FileChannel, configuration);
		await CreateFileWorker ();
		
		if (SyncDirection == SyncDirection.ToXcode)
			await SyncToXcodeAsync (token).ConfigureAwait (false);
		else
			await SyncFromXcodeAsync (token).ConfigureAwait (false);

		Logger.Debug ("Synchronization complete.");
	}

	async Task SyncToXcodeAsync (CancellationToken token)
	{
		Logger.Debug ("Generating Xcode project files...");

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

		foreach (var t in TypeService.QueryTypes ()) {
			if (t is null) continue;

			// all NSObjects get a corresponding .h + .m file generated
			var genH = new GenObjcH (t).TransformText ();

			await WriteFile (FileSystem.Path.Combine (TargetDir, t.ObjCType + ".h"), genH);

			var genM = new GenObjcM (t).TransformText ();
			await WriteFile (FileSystem.Path.Combine (TargetDir, t.ObjCType + ".m"), genM);

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
					path.EndsWith (".storyboard", StringComparison.OrdinalIgnoreCase)
				).ToList ();

		// support for maui apps
		string altPath = targetPlatform switch {
			"ios" => FileSystem.Path.Combine (FileSystem.Path.GetDirectoryName (ProjectPath)!, "Platforms", "iOS"),
			"maccatalyst" => FileSystem.Path.Combine (FileSystem.Path.GetDirectoryName (ProjectPath)!, "Platforms", "MacCatalyst"),
			_ => ""
		};

		var appleDirectory = FileSystem.Path.Exists (altPath) ? altPath : FileSystem.Path.GetDirectoryName (ProjectPath)!;

		foreach (var file in appleFiles) {
			FileSystem.File.Copy (file, FileSystem.Path.Combine (TargetDir, FileSystem.Path.GetFileName (file)), true);
		}

		// copy assets
		// single plat project support
		var assetsFolder = FileSystem.Directory
			.EnumerateDirectories (appleDirectory, "*.xcassets", SearchOption.TopDirectoryOnly).FirstOrDefault ();
		if (assetsFolder is not null)
			Scripts.CopyDirectory (FileSystem, assetsFolder, FileSystem.Path.Combine (TargetDir, FileSystem.Path.GetFileName (assetsFolder)), true);

		// maui support
		foreach (var asset in Scripts.GetAssets (FileSystem, ProjectPath, Framework)) {
			Scripts.CopyDirectory (FileSystem, asset, FileSystem.Path.Combine (TargetDir, "Assets.xcassets"), true);
		}

		// create in memory representation of Xcode assets
		var xcodeObjects = new Dictionary<string, XcodeObject> ();
		var pbxSourcesBuildFiles = new List<string> ();
		var pbxResourcesBuildFiles = new List<string> ();
		var pbxFrameworksBuildFiles = new List<string> ();
		var pbxGroupFiles = new List<string> ();

		var projectName = FileSystem.Path.GetFileNameWithoutExtension (ProjectPath);

		// for each file in target directory create FileReference and add to PBXResourcesBuildPhase
		foreach (var file in FileSystem.Directory.GetFiles (TargetDir)) {

			var fileReference = new PBXFileReference ();
			var buildFile = new PBXBuildFile ();

			switch (FileSystem.Path.GetExtension (file).ToLower ()) {
			case ".h":
				fileReference = new PBXFileReference {
					Isa = "PBXFileReference",
					LastKnownFileType = "sourcecode.c.h",
					Name = FileSystem.Path.GetFileName (file),
					Path = FileSystem.Path.GetFileName (file),
					SourceTree = "<group>"
				};
				xcodeObjects.Add (fileReference.Token, fileReference);
				pbxGroupFiles.Add (fileReference.Token);

				// no build file for header files
				break;
			case ".m":
				fileReference = new PBXFileReference {
					Isa = "PBXFileReference",
					LastKnownFileType = "sourcecode.c.objc",
					Name = FileSystem.Path.GetFileName (file),
					Path = FileSystem.Path.GetFileName (file),
					SourceTree = "<group>"
				};
				xcodeObjects.Add (fileReference.Token, fileReference);
				pbxGroupFiles.Add (fileReference.Token);

				buildFile = new PBXBuildFile {
					Isa = "PBXBuildFile",
					FileRef = fileReference.Token
				};

				xcodeObjects.Add (buildFile.Token, buildFile);
				pbxSourcesBuildFiles.Add (buildFile.Token);
				break;
			case ".plist":
			case ".storyboard":
				// add to resources build phase
				fileReference = new PBXFileReference {
					Isa = "PBXFileReference",
					LastKnownFileType = "text.plist.xml",
					Name = FileSystem.Path.GetFileName (file),
					Path = FileSystem.Path.GetFileName (file),
					SourceTree = "<group>"
				};
				xcodeObjects.Add (fileReference.Token, fileReference);
				pbxGroupFiles.Add (fileReference.Token);

				buildFile = new PBXBuildFile {
					Isa = "PBXBuildFile",
					FileRef = fileReference.Token
				};

				xcodeObjects.Add (buildFile.Token, buildFile);
				pbxResourcesBuildFiles.Add (buildFile.Token);
				break;
			}
		}

		// add a file reference for the *.app item
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
				releaseBuildConfiguration.Token ],
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
			string workspacePath = FileSystem.Path.Combine (TargetDir, projectName + ".xcodeproj", "project.xcworkspace");
			Logger?.Information (Strings.Generate.OpenProject (Scripts.Run (Scripts.OpenXcodeProject (workspacePath))));
		}
		return;
	}

	async Task SyncFromXcodeAsync (CancellationToken token)
	{
		Logger.Information (Strings.Sync.HeaderInformation, TargetDir, ProjectPath);

		var projectName = FileSystem.Path.GetFileNameWithoutExtension (ProjectPath);

		var dotNetProject = new ClrProject (FileSystem, Logger, TypeService, projectName, ProjectPath, Framework);
		await dotNetProject.OpenProject ().ConfigureAwait (false);

		var xcodeWorkspace = new XcodeWorkspace (FileSystem, Logger, TypeService, projectName, TargetDir, Framework);

		// TODO: After this executes the TypeService has been updated with new SyntaxTrees
		await xcodeWorkspace.LoadAsync (token).ConfigureAwait (false);

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
			// TODO: This should probably be extracted to a different class, like SyncableFile/Type
			await WriteFile (syntaxTree!.FilePath, syntaxTree?.GetRoot (token).GetText().ToString () ?? string.Empty);
		}
	}

	public async Task CreateFileWorker () {
		var tcs = new TaskCompletionSource<bool> ();
		var worker = new FileWorker (Logger, tcs, FileSystem);
		await Hub.RegisterAsync (FileChannel, worker);
	}

	public async Task WriteFile (string path, string content) =>
		await Hub.Publish (FileChannel, new FileMessage {
			Id = Guid.NewGuid ().ToString (),
			Path = path,
			Content = content
		});
}
