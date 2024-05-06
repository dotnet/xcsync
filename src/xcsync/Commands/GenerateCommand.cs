// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using xcsync.Projects;
using xcsync.Projects.Xcode;

namespace xcsync.Commands;

public class GenerateCommand : XcodeCommand<GenerateCommand> {
	public GenerateCommand () : base ("generate",
			"generate a Xcode project at the path specified by --target from the project identified by --project")
	{
		this.SetHandler (Execute);
	}

	public async Task Execute ()
	{
		Logger?.Information (Strings.Generate.HeaderInformation, ProjectPath, TargetPath);

		if (!TryGetTargetPlatform (Tfm, out string targetPlatform))
			return;

		var dotnet = new Dotnet (ProjectPath, Tfm);
		var nsProject = new NSProject (dotnet, targetPlatform);
		HashSet<string> frameworks = ["Foundation", "Cocoa"];

		// match target platform to build settings id
		(string sdkroot, string deployment) = Tfm switch {
			"macos" => ("macosx", "macosx"),
			"ios" => ("iphoneos", "iphoneos"),
			"maccatalyst" => ("iphoneos", "iphoneos"),
			"tvos" => ("appletvos", "tvos"),
			_ => ("", ""),
		};

		await foreach (var t in nsProject.GetTypes ()) {
			// all NSObjects get a corresponding .h + .m file generated
			var gen = new GenObjcH (t).TransformText ();
			await File.WriteAllTextAsync (Path.Combine (TargetPath, t.ObjCType + ".h"), gen).ConfigureAwait (false);

			var genM = new GenObjcM (t).TransformText ();
			await File.WriteAllTextAsync (Path.Combine (TargetPath, t.ObjCType + ".m"), genM).ConfigureAwait (false);

			// add references for framework resolution at project level
			foreach (var r in t.References) {
				frameworks.Add (r);
			}
		}

		Logger?.Debug ($"Generated nsProject header and implementation files");

		// copy storyboard, entitlements/info.plist files to the target directory 
		var ext = new List<string> { "storyboard", "plist" };

		// support for maui apps
		string altPath = targetPlatform switch {
			"ios" => Path.Combine (Path.GetDirectoryName (ProjectPath)!, "Platforms", "iOS"),
			"maccatalyst" => Path.Combine (Path.GetDirectoryName (ProjectPath)!, "Platforms", "MacCatalyst"),
			_ => ""
		};

		var appleDirectory = Path.Exists (altPath) ? altPath : Path.GetDirectoryName (ProjectPath)!;

		var appleFiles = Directory
			.EnumerateFiles (appleDirectory, "*.*", SearchOption.TopDirectoryOnly)
			.Where (s => ext.Contains (Path.GetExtension (s).TrimStart ('.').ToLowerInvariant ()));

		foreach (var file in appleFiles) {
			File.Copy (file, Path.Combine (TargetPath, Path.GetFileName (file)), true);
		}

		// create in memory representation of Xcode assets
		var xcodeObjects = new Dictionary<string, XcodeObject> ();
		var pbxSourcesBuildFiles = new List<string> ();
		var pbxResourcesBuildFiles = new List<string> ();
		var pbxFrameworksBuildFiles = new List<string> ();
		var pbxGroupFiles = new List<string> ();

		var projectName = Path.GetFileNameWithoutExtension (ProjectPath);

		// for each file in target directory create FileReference and add to PBXResourcesBuildPhase
		foreach (var file in Directory.GetFiles (TargetPath)) {

			var fileReference = new PBXFileReference ();
			var buildFile = new PBXBuildFile ();

			switch (Path.GetExtension (file).ToLower ()) {
			case ".h":
				fileReference = new PBXFileReference {
					Isa = "PBXFileReference",
					LastKnownFileType = "sourcecode.c.h",
					Name = Path.GetFileName (file),
					Path = Path.GetFileName (file),
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
					Name = Path.GetFileName (file),
					Path = Path.GetFileName (file),
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
					Name = Path.GetFileName (file),
					Path = Path.GetFileName (file),
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
				Name = Path.GetFileName (path),
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
				productsGroup.Token, frameworksGroup.Token, projectGroup.Token
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
				{$"{deployment.ToUpper()}_DEPLOYMENT_TARGET", new List<string> {"14.0"}},
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
				{$"{deployment.ToUpper()}_DEPLOYMENT_TARGET", new List<string> {"14.0"}},
				{"MTL_ENABLE_DEBUG_INFO", new List<string> {"NO"}},
				{"SDKROOT", new List<string> {sdkroot}},
				{"VALIDATE_PRODUCT", new List<string> {"YES"}},
			},
			Name = "Release",
		};
		xcodeObjects.Add (releaseBuildConfiguration.Token, releaseBuildConfiguration);

		var commonTargetBuildSettings = new Dictionary<string, IList<string>> {
			{ "INFOPLIST_FILE", new List<string> { "Info.plist" } },
			{ "PRODUCT_NAME", new List<string> { "$(TARGET_NAME)" } },
			{ "ASSETCATALOG_COMPILER_APPICON_NAME", new List<string> { "AppIcon" } },
			{ "CODE_SIGN_STYLE", new List<string> { "Automatic" } },
			{ "LD_RUNPATH_SEARCH_PATHS", new List<string> { "$(inherited)", "@executable_path/../Frameworks" } }, {
				"PRODUCT_BUNDLE_IDENTIFIER",
				new List<string> { $"com.companyname.{projectName}" }
			},
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
				releaseTargetBuildConfiguration.Token ],
			DefaultConfigurationName = "Debug",
			DefaultConfigurationIsVisible = "0",
		};
		xcodeObjects.Add (targetBuildCombo.Token, targetBuildCombo);

		var buildCombo = new XCConfigurationList {
			Isa = "XCConfigurationList",
			BuildConfigurations = new List<string> {
				debugBuildConfiguration.Token,
				releaseBuildConfiguration.Token },
			DefaultConfigurationName = "Debug",
			DefaultConfigurationIsVisible = "0",
		};
		xcodeObjects.Add (buildCombo.Token, buildCombo);

		var nativeTarget = new PBXNativeTarget {
			Isa = "PBXNativeTarget",
			BuildConfigurationList = targetBuildCombo.Token,
			BuildPhases = new List<string> { pbxSourcesBuildPhase.Token, pbxResourcesBuildPhase.Token, pbxFrameworksBuildPhase.Token },
			BuildRules = new List<object> (),
			Dependencies = new List<object> (),
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
			KnownRegions = new List<string> { "en" },
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
		XcodeWorkspaceGenerator.Generate (projectName, Environment.UserName, TargetPath, xcodeProject);
		Logger?.Information ($"Generated Xcode project at '{TargetPath}'");

		if (Open) {
			string workspacePath = Path.Combine (TargetPath, projectName + ".xcodeproj", "project.xcworkspace");
			Logger?.Information (Scripts.Run (Scripts.OpenXcodeProject (workspacePath)) + " is open in Xcode");
		}
	}

	static bool TryGetTargetPlatform (string tfm, /* List<string> supportedTfms, */ [NotNullWhen (true)] out string targetPlatform)
	{
		targetPlatform = string.Empty;

		foreach (var platform in xcSync.ApplePlatforms) {
			if (tfm.Contains (platform.Key)) {
				targetPlatform = platform.Key;
				return true;
			}
		}

		Logger?.Fatal (Strings.Errors.TargetPlatformNotFound);
		return false;
	}
}
