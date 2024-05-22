// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;
using Xamarin;
using xcsync.Projects.Xcode;
using Xunit.Abstractions;

namespace xcsync.tests.Projects;

public class XcodeProjectTest (ITestOutputHelper TestOutput) : Base {

	[Fact]
	public void Deserialize_RootObject_FromJson_Succeeds ()
	{
		// Arrange
		string json = @"
        {
            ""classes"": {},
            ""objectVersion"": ""50"",
            ""archiveVersion"": ""1"",
            ""objects"": {},
            ""rootObject"": ""898C72000000000000000000""

        }";

		// Act
		XcodeProject? project = JsonSerializer.Deserialize<XcodeProject> (json);

		// Assert
		Assert.NotNull (project);
		Assert.NotNull (project.Classes);
		Assert.Equal ("50", project.ObjectVersion);
		Assert.Equal ("1", project.ArchiveVersion);
		Assert.NotNull (project.Objects);
		Assert.Equal ("898C72000000000000000000", project.RootObject);
	}

	[Fact]
	public void Deserialize_PbxProject_FromJson_Succeeds ()
	{
		// Arrange
		string json = @"
        {
            ""classes"": {},
            ""objectVersion"": ""50"",
            ""archiveVersion"": ""1"",
            ""objects"": {
                ""898C72000000000000000000"": {
                    ""buildConfigurationList"": ""D5F008000000000000000000"",
                    ""targets"": [
                        ""35A953000000000000000000""
                    ],
                    ""developmentRegion"": ""en"",
                    ""knownRegions"": [
                        ""en""
                    ],
                    ""isa"": ""PBXProject"",
                    ""compatibilityVersion"": ""Xcode 9.3"",
                    ""productRefGroup"": ""1ABC8F300000000000000000"",
                    ""projectDirPath"": """",
                    ""attributes"": {
                        ""LastUpgradeCheck"": ""1520""
                    },
                    ""hasScannedForEncodings"": ""0"",
                    ""projectRoot"": """",
                    ""mainGroup"": ""DBB6E1000000000000000000""
                }
            },
            ""rootObject"": ""898C72000000000000000000""

        }";

		// Act
		XcodeProject? project = JsonSerializer.Deserialize<XcodeProject> (json);

		// Assert
		Assert.NotNull (project);
		Assert.NotNull (project.Classes);
		Assert.Equal ("50", project.ObjectVersion);
		Assert.Equal ("1", project.ArchiveVersion);
		Assert.NotNull (project.Objects);
		Assert.Single (project.Objects);
		Assert.Equal ("898C72000000000000000000", project.RootObject);

		Assert.Equal ("PBXProject", project.Objects ["898C72000000000000000000"].Isa);
		Assert.Equal ("PBXProject", project.Objects ["898C72000000000000000000"].GetType ().Name);
		Assert.Equal ("898C72000000000000000000", project.Objects ["898C72000000000000000000"].Token);

		var pbxProject = project.Objects ["898C72000000000000000000"] as PBXProject;
		Assert.NotNull (pbxProject);
		Assert.Equal ("Xcode 9.3", pbxProject.CompatibilityVersion);
		Assert.Equal ("en", pbxProject.DevelopmentRegion);
		Assert.Equal ("0", pbxProject.HasScannedForEncodings);
		Assert.Equal ("DBB6E1000000000000000000", pbxProject.MainGroup);
		Assert.Equal ("1ABC8F300000000000000000", pbxProject.ProductRefGroup);
		Assert.Equal ("", pbxProject.ProjectDirPath);
		Assert.Equal ("", pbxProject.ProjectRoot);
		Assert.NotNull (pbxProject.Attributes);
		Assert.Equal ("1520", pbxProject.Attributes ["LastUpgradeCheck"]);
		Assert.NotNull (pbxProject.KnownRegions);
		Assert.Single (pbxProject.KnownRegions);
		Assert.Equal ("en", pbxProject.KnownRegions [0]);
	}

	[Fact]
	public void Deserialize_PbxFileReference_FromJson_Succeeds ()
	{
		// Arrange
		string json = @"
        {
            ""classes"": {},
            ""objectVersion"": ""50"",
            ""archiveVersion"": ""1"",
            ""objects"": {
                ""3D58D9400000000000000000"": {
                    ""path"": ""ViewController.m"",
                    ""isa"": ""PBXFileReference"",
                    ""name"": ""ViewController.m"",
                    ""lastKnownFileType"": ""sourcecode.c.objc"",
                    ""sourceTree"": ""<group>""
                }
            },
            ""rootObject"": ""898C72000000000000000000""

        }";

		// Act
		XcodeProject? project = JsonSerializer.Deserialize<XcodeProject> (json);

		// Assert
		Assert.NotNull (project);
		Assert.NotNull (project.Classes);
		Assert.Equal ("50", project.ObjectVersion);
		Assert.Equal ("1", project.ArchiveVersion);
		Assert.NotNull (project.Objects);
		Assert.Single (project.Objects);
		Assert.Equal ("898C72000000000000000000", project.RootObject);

		Assert.Equal ("PBXFileReference", project.Objects ["3D58D9400000000000000000"].Isa);
		Assert.Equal ("PBXFileReference", project.Objects ["3D58D9400000000000000000"].GetType ().Name);
		Assert.Equal ("3D58D9400000000000000000", project.Objects ["3D58D9400000000000000000"].Token);

		var pbxFileReference = project.Objects ["3D58D9400000000000000000"] as PBXFileReference;
		Assert.NotNull (pbxFileReference);
		Assert.Equal ("ViewController.m", pbxFileReference.Path);
		Assert.Equal ("ViewController.m", pbxFileReference.Name);
		Assert.Equal ("sourcecode.c.objc", pbxFileReference.LastKnownFileType);
		Assert.Equal ("<group>", pbxFileReference.SourceTree);
	}

	[Fact]
	public void Deserialize_XCBuildConfiguration_FromJson_Succeeds ()
	{
		// Arrange
		string json = @"
        {
            ""classes"": {},
            ""objectVersion"": ""50"",
            ""archiveVersion"": ""1"",
            ""objects"": {
                ""385704800000000000000000"": {
                    ""isa"": ""XCBuildConfiguration"",
                    ""buildSettings"": {
                        ""CLANG_WARN_UNGUARDED_AVAILABILITY"": ""YES_AGGRESSIVE"",
                        ""CLANG_WARN_SUSPICIOUS_MOVE"": ""YES"",
                        ""CLANG_WARN_DIRECT_OBJC_ISA_USAGE"": ""YES_ERROR"",
                        ""CLANG_ENABLE_OBJC_ARC"": ""YES"",
                        ""CLANG_ENABLE_OBJC_WEAK"": ""YES"",
                        ""CLANG_WARN__DUPLICATE_METHOD_MATCH"": ""YES"",
                        ""GCC_WARN_UNDECLARED_SELECTOR"": ""YES"",
                        ""IPHONEOS_DEPLOYMENT_TARGET"": ""16.4"",
                        ""CLANG_WARN_INFINITE_RECURSION"": ""YES"",
                        ""DEBUG_INFORMATION_FORMAT"": ""dwarf"",
                        ""CLANG_WARN_OBJC_IMPLICIT_RETAIN_SELF"": ""YES"",
                        ""CLANG_ANALYZER_NUMBER_OBJECT_CONVERSION"": ""YES_AGGRESSIVE"",
                        ""SDKROOT"": ""iphoneos"",
                        ""CLANG_CXX_LANGUAGE_STANDARD"": ""gnu++17"",
                        ""ENABLE_TESTABILITY"": ""YES"",
                        ""CLANG_WARN_BOOL_CONVERSION"": ""YES"",
                        ""CLANG_WARN_UNREACHABLE_CODE"": ""YES"",
                        ""ENABLE_STRICT_OBJC_MSGSEND"": ""YES"",
                        ""CLANG_ANALYZER_NONNULL"": ""YES"",
                        ""CLANG_WARN_BLOCK_CAPTURE_AUTORELEASING"": ""YES"",
                        ""CLANG_WARN_STRICT_PROTOTYPES"": ""YES"",
                        ""CLANG_WARN_ENUM_CONVERSION"": ""YES"",
                        ""CLANG_WARN_QUOTED_INCLUDE_IN_FRAMEWORK_HEADER"": ""YES"",
                        ""GCC_PREPROCESSOR_DEFINITIONS"": [
                            ""DEBUG=1"",
                            ""$(inherited)""
                        ],
                        ""GCC_WARN_64_TO_32_BIT_CONVERSION"": ""YES"",
                        ""CLANG_WARN_EMPTY_BODY"": ""YES"",
                        ""GCC_WARN_UNINITIALIZED_AUTOS"": ""YES_AGGRESSIVE"",
                        ""GCC_WARN_ABOUT_RETURN_TYPE"": ""YES_ERROR"",
                        ""CLANG_WARN_COMMA"": ""YES"",
                        ""GCC_DYNAMIC_NO_PIC"": ""NO"",
                        ""GCC_C_LANGUAGE_STANDARD"": ""gnu11"",
                        ""GCC_WARN_UNUSED_VARIABLE"": ""YES"",
                        ""CLANG_WARN_RANGE_LOOP_ANALYSIS"": ""YES"",
                        ""CLANG_ENABLE_MODULES"": ""YES"",
                        ""CLANG_WARN_INT_CONVERSION"": ""YES"",
                        ""ALWAYS_SEARCH_USER_PATHS"": ""NO"",
                        ""CLANG_WARN_OBJC_ROOT_CLASS"": ""YES_ERROR"",
                        ""CLANG_WARN_DEPRECATED_OBJC_IMPLEMENTATIONS"": ""YES"",
                        ""COPY_PHASE_STRIP"": ""NO"",
                        ""CLANG_WARN_CONSTANT_CONVERSION"": ""YES"",
                        ""GCC_NO_COMMON_BLOCKS"": ""YES"",
                        ""GCC_OPTIMIZATION_LEVEL"": ""0"",
                        ""MTL_ENABLE_DEBUG_INFO"": ""INCLUDE_SOURCE"",
                        ""ONLY_ACTIVE_ARCH"": ""YES"",
                        ""CLANG_WARN_NON_LITERAL_NULL_CONVERSION"": ""YES"",
                        ""MTL_FAST_MATH"": ""YES"",
                        ""GCC_WARN_UNUSED_FUNCTION"": ""YES"",
                        ""CLANG_WARN_OBJC_LITERAL_CONVERSION"": ""YES"",
                        ""CLANG_WARN_DOCUMENTATION_COMMENTS"": ""YES"",
                        ""CLANG_CXX_LIBRARY"": ""libc++""
                    },
                    ""name"": ""Debug""
                }
            },
            ""rootObject"": ""898C72000000000000000000""

        }";
		JsonSerializerOptions options = new () {
			WriteIndented = true
		};

		// Act
		XcodeProject? project = JsonSerializer.Deserialize<XcodeProject> (json, options);

		// Assert
		Assert.NotNull (project);
		Assert.NotNull (project.Classes);
		Assert.Equal ("50", project.ObjectVersion);
		Assert.Equal ("1", project.ArchiveVersion);
		Assert.NotNull (project.Objects);
		Assert.Single (project.Objects);
		Assert.Equal ("898C72000000000000000000", project.RootObject);

		Assert.Equal ("XCBuildConfiguration", project.Objects ["385704800000000000000000"].Isa);
		Assert.Equal ("XCBuildConfiguration", project.Objects ["385704800000000000000000"].GetType ().Name);
		Assert.Equal ("385704800000000000000000", project.Objects ["385704800000000000000000"].Token);

		var xcBuildConfiguration = project.Objects ["385704800000000000000000"] as XCBuildConfiguration;
		Assert.NotNull (xcBuildConfiguration);

		Assert.Equal ("Debug", xcBuildConfiguration.Name);
		Assert.NotNull (xcBuildConfiguration.BuildSettings);
		Assert.Equal ("YES_AGGRESSIVE", xcBuildConfiguration.BuildSettings ["CLANG_WARN_UNGUARDED_AVAILABILITY"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_WARN_SUSPICIOUS_MOVE"].FirstOrDefault ());
		Assert.Equal ("YES_ERROR", xcBuildConfiguration.BuildSettings ["CLANG_WARN_DIRECT_OBJC_ISA_USAGE"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_ENABLE_OBJC_ARC"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_ENABLE_OBJC_WEAK"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_WARN__DUPLICATE_METHOD_MATCH"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["GCC_WARN_UNDECLARED_SELECTOR"].FirstOrDefault ());
		Assert.Equal ("16.4", xcBuildConfiguration.BuildSettings ["IPHONEOS_DEPLOYMENT_TARGET"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_WARN_INFINITE_RECURSION"].FirstOrDefault ());
		Assert.Equal ("dwarf", xcBuildConfiguration.BuildSettings ["DEBUG_INFORMATION_FORMAT"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_WARN_OBJC_IMPLICIT_RETAIN_SELF"].FirstOrDefault ());
		Assert.Equal ("YES_AGGRESSIVE", xcBuildConfiguration.BuildSettings ["CLANG_ANALYZER_NUMBER_OBJECT_CONVERSION"].FirstOrDefault ());
		Assert.Equal ("iphoneos", xcBuildConfiguration.BuildSettings ["SDKROOT"].FirstOrDefault ());
		Assert.Equal ("gnu++17", xcBuildConfiguration.BuildSettings ["CLANG_CXX_LANGUAGE_STANDARD"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["ENABLE_TESTABILITY"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_WARN_BOOL_CONVERSION"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_WARN_UNREACHABLE_CODE"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["ENABLE_STRICT_OBJC_MSGSEND"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_ANALYZER_NONNULL"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_WARN_BLOCK_CAPTURE_AUTORELEASING"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_WARN_STRICT_PROTOTYPES"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_WARN_ENUM_CONVERSION"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_WARN_QUOTED_INCLUDE_IN_FRAMEWORK_HEADER"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["GCC_WARN_64_TO_32_BIT_CONVERSION"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_WARN_EMPTY_BODY"].FirstOrDefault ());
		Assert.Equal ("YES_AGGRESSIVE", xcBuildConfiguration.BuildSettings ["GCC_WARN_UNINITIALIZED_AUTOS"].FirstOrDefault ());
		Assert.Equal ("YES_ERROR", xcBuildConfiguration.BuildSettings ["GCC_WARN_ABOUT_RETURN_TYPE"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_WARN_COMMA"].FirstOrDefault ());
		Assert.Equal ("NO", xcBuildConfiguration.BuildSettings ["GCC_DYNAMIC_NO_PIC"].FirstOrDefault ());
		Assert.Equal ("gnu11", xcBuildConfiguration.BuildSettings ["GCC_C_LANGUAGE_STANDARD"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["GCC_WARN_UNUSED_VARIABLE"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_WARN_RANGE_LOOP_ANALYSIS"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_ENABLE_MODULES"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_WARN_INT_CONVERSION"].FirstOrDefault ());
		Assert.Equal ("NO", xcBuildConfiguration.BuildSettings ["ALWAYS_SEARCH_USER_PATHS"].FirstOrDefault ());
		Assert.Equal ("YES_ERROR", xcBuildConfiguration.BuildSettings ["CLANG_WARN_OBJC_ROOT_CLASS"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_WARN_DEPRECATED_OBJC_IMPLEMENTATIONS"].FirstOrDefault ());
		Assert.Equal ("NO", xcBuildConfiguration.BuildSettings ["COPY_PHASE_STRIP"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_WARN_CONSTANT_CONVERSION"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["GCC_NO_COMMON_BLOCKS"].FirstOrDefault ());
		Assert.Equal ("0", xcBuildConfiguration.BuildSettings ["GCC_OPTIMIZATION_LEVEL"].FirstOrDefault ());
		Assert.Equal ("INCLUDE_SOURCE", xcBuildConfiguration.BuildSettings ["MTL_ENABLE_DEBUG_INFO"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["ONLY_ACTIVE_ARCH"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_WARN_NON_LITERAL_NULL_CONVERSION"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["MTL_FAST_MATH"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["GCC_WARN_UNUSED_FUNCTION"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_WARN_OBJC_LITERAL_CONVERSION"].FirstOrDefault ());
		Assert.Equal ("YES", xcBuildConfiguration.BuildSettings ["CLANG_WARN_DOCUMENTATION_COMMENTS"].FirstOrDefault ());
		Assert.Equal ("libc++", xcBuildConfiguration.BuildSettings ["CLANG_CXX_LIBRARY"].FirstOrDefault ());

		Assert.Equal (2, xcBuildConfiguration.BuildSettings ["GCC_PREPROCESSOR_DEFINITIONS"].Count);
		Assert.Equal ("DEBUG=1", xcBuildConfiguration.BuildSettings ["GCC_PREPROCESSOR_DEFINITIONS"] [0]);
		Assert.Equal ("$(inherited)", xcBuildConfiguration.BuildSettings ["GCC_PREPROCESSOR_DEFINITIONS"] [1]);

	}

	[Fact]
	public void Deserialize_ThenSerialize_GeneratesSameJson ()
	{
		// Arrange
		string testFilePath = Path.Combine (Environment.CurrentDirectory, "..", "..", "..", "Resources", "SampleProject.json");
		string json = File.ReadAllText (testFilePath);

		var options = new JsonSerializerOptions {
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		};

		// Act
		XcodeProject? project = JsonSerializer.Deserialize<XcodeProject> (json, options);
		var jsonString = JsonSerializer.Serialize (project, options);

		// Assert
		Assert.NotNull (jsonString);
		Assert.Equal (json, jsonString);
	}

	[Theory]
	[InlineData ("macos", "", "net8.0-macos", new string [] { "AppDelegate", "Info.plist", "Main.storyboard", "ViewController" })]
	[InlineData ("maccatalyst", "", "net8.0-maccatalyst", new string [] { "AppDelegate", "Info.plist", "SceneDelegate" })]
	[InlineData ("ios", "", "net8.0-ios", new string [] { "AppDelegate", "Info.plist", "LaunchScreen.storyboard", "SceneDelegate" })]
	[InlineData ("tvos", "", "net8.0-tvos", new string [] { "AppDelegate", "Info.plist", "Main.storyboard", "ViewController" })]
	[InlineData ("maui", "", "net8.0-ios", new string [] { "AppDelegate", "Info.plist" })]
	[InlineData ("maui", "", "net8.0-maccatalyst", new string [] { "AppDelegate", "Info.plist" })]
	public void IsXcodeProjectGenerated (string projectType, string templateOptions, string tfm, string [] projectFiles)
	{
		// testing entirety of generate command
		// dotnet new macos > xcsync > verify

		var projectName = Guid.NewGuid ().ToString ();
		var tmpDir = Cache.CreateTemporaryDirectory (projectName);

		// Run 'dotnet new macos' in temp dir
		// DotnetNew (TestOutput, "globaljson", tmpDir, "--sdk-version 8.0.0 --roll-forward feature");
		DotnetNew (TestOutput, projectType, tmpDir, templateOptions);

		Assert.True (Directory.Exists (tmpDir));

		var xcodeDir = Path.Combine (tmpDir, "xcode");
		Directory.CreateDirectory (xcodeDir);

		var csproj = Path.Combine (tmpDir, $"{projectName}.csproj");

		// Run 'xcsync generate'
		Xcsync (TestOutput, "generate", "--project", csproj, "--target", xcodeDir, "-tfm", tfm);

		projectFiles.SelectMany (projectFile => {
			return (IEnumerable<string>) (Path.HasExtension (projectFile) ? ([projectFile]) : ([$"{projectFile}.m", $"{projectFile}.h"]));
		}).Union ([
			 Path.Combine (xcodeDir, $"{Path.GetFileName (projectName)}.xcodeproj", "project.xcworkspace", "xcuserdata", $"{Environment.UserName}.xcuserdatad", "WorkspaceSettings.xcsettings"),
			Path.Combine (xcodeDir, $"{Path.GetFileName (projectName)}.xcodeproj", "project.xcworkspace", "contents.xcworkspacedata"),
			Path.Combine (xcodeDir, $"{Path.GetFileName (projectName)}.xcodeproj", "project.pbxproj"),
		]).ToList ().ForEach (file => {
			var fullPathToFile = Path.Combine (xcodeDir, file);
			Assert.True (File.Exists (fullPathToFile), $"{fullPathToFile} does not exist");
		});
	}

	[Fact]
	[Trait ("Category", "XcodeIntegration")]
	public void IsXcodeProjectOpen ()
	{
		// Assert to make sure Xcode successfully opens project when --open flag used
		var projectName = Guid.NewGuid ().ToString ();
		var tmpDir = Cache.CreateTemporaryDirectory (projectName);

		// Run 'dotnet new macos' in temp dir
		DotnetNew (TestOutput, "macos", tmpDir);

		Assert.True (Directory.Exists (Path.Combine (tmpDir)));

		var xcodeDir = Path.Combine (tmpDir, "xcode");
		Directory.CreateDirectory (xcodeDir);
		var csproj = Path.Combine (tmpDir, $"{projectName}.csproj");
		string projectPath = Path.Combine (xcodeDir, $"{Path.GetFileName (projectName)}.xcodeproj");

		try {
			// Run 'xcsync generate'
			Xcsync (TestOutput, "generate", "--project", csproj, "--target", xcodeDir, "--open");

			// check if xcode has project open

			string openResult = Scripts.Run (Scripts.CheckXcodeProject (projectPath));

			Assert.Equal ("true", openResult);
		} finally {
			Scripts.Run (Scripts.CloseXcodeProject (projectPath));
		}
	}
}
