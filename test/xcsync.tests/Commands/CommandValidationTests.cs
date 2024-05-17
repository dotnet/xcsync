// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO.Abstractions.TestingHelpers;
using Xamarin;
using xcsync.Commands;
using Xunit.Abstractions;

namespace xcsync.tests.Commands;

public class CommandValidationTests (ITestOutputHelper TestOutput) : Base {

	[Fact]
	public void TestBaseCommandCreation ()
	{
		var fileSystem = new MockFileSystem ();

		var baseCommand = new BaseCommand<string> (fileSystem, "test", "test description");
		Assert.NotNull (baseCommand);
	}

	[Fact]
	public void TestXcodeCommandCreation ()
	{
		var fileSystem = new MockFileSystem ();

		var baseCommand = new XcodeCommand<string> (fileSystem, "test", "test description");
		Assert.NotNull (baseCommand);
	}

	[Theory]
	[InlineData ("macos", "", "", "")]
	[InlineData ("macos", "net8.0-macos", "", "")]
	[InlineData ("macos", "net8.0-macos", "obj/xcode", "")]
	[InlineData ("macos", "net9.0-macos", "obj/xcode", "Target framework is not supported by current .net project.")]
	[InlineData ("maui", "", "", "Multiple target frameworks found in the project, please specify one using [--target-framework-moniker, -tfm] option.")]
	[InlineData ("maui", "net8.0-ios", "obj/xcode", "")]
	[InlineData ("maui", "net8.0-maccatalyst", "obj/xcode", "")]
	[InlineData ("maui", "net8.0-macos", "obj/xcode", "Target framework is not supported by current .net project.")]
	[InlineData ("maui", "net8.0-ios", "{Directory}/xcode", "Target path '{TargetPath}' does not exist, will create directory if [--force, -f] is set.")]
	public void BaseCommandValidation_SingleProject (string projectType, string tfm, string targetPath, string expectedError)
	{
		var projectTypes = new Dictionary<string, string> {
			{ "ios", net_8_0_iosProject },
			{"macos", net_8_0_macosProject},
			{ "maui", mauiProject }
		};

		var projectName = Guid.NewGuid ().ToString ();
		var projectDir = projectType;

		var fileSystem = new MockFileSystem (
		// new Dictionary<string, MockFileData> {
		// 	{ fullProjectPath, new MockFileData (projectTypes[projectType]) },
		// }
		);
		var fullProjectPath = fileSystem.Path.Combine (projectDir, $"{projectName}.csproj");
		fileSystem.AddFile (fullProjectPath, new MockFileData (projectTypes [projectType]));

		targetPath = targetPath
			.Replace ("{Directory}", fileSystem.Path.GetDirectoryName (fullProjectPath));

		expectedError = expectedError
			.Replace ("{Directory}", fileSystem.File.Exists (fullProjectPath) ? fileSystem.Path.GetDirectoryName (fullProjectPath) : fullProjectPath)
			.Replace ("{CsProjFile}", fullProjectPath)
			.Replace ("{TargetFramework}", tfm)
			.Replace ("{TargetPath}", targetPath);

		var baseCommand = new BaseCommand<string> (fileSystem, "test", "test description", fullProjectPath, tfm, targetPath);

		var validation = baseCommand.ValidateCommand (fullProjectPath, tfm, targetPath);

		Assert.Equal (expectedError, validation.Error);
		if (string.IsNullOrEmpty (expectedError)) {
			Assert.NotEmpty (validation.ProjectPath);
			Assert.EndsWith (".csproj", validation.ProjectPath);
			Assert.NotEmpty (validation.Tfm);
			Assert.NotEmpty (validation.TargetPath);
			Assert.True (fileSystem.Directory.Exists (validation.TargetPath));
		}
	}

	[Theory]
	[InlineData ("", "Multiple .csproj files found in '{CsProjFile}', please specify the project file to use:")]
	[InlineData ("does-not-exist.csproj", "File not found: '{CsProjFile}'")]
	public void BaseCommandValidation_MultipleProjects (string projectNameParam, string expectedError)
	{
		var projectName = Guid.NewGuid ().ToString ();
		var projectDir = "/src";

		var fileSystem = new MockFileSystem (
			new Dictionary<string, MockFileData> {
				{ Path.Combine (projectDir, $"{projectName}.csproj"), new MockFileData ("") },
				{ Path.Combine (projectDir, $"{projectName}-2.csproj"), new MockFileData ("") }
			}
		);

		var project1Path = fileSystem.Path.Combine (projectDir, $"{projectName}.csproj");
		var project2Path = fileSystem.Path.Combine (projectDir, $"{projectName}-2.csproj");

		var fullProjectPath = fileSystem.Path.Combine (projectDir, $"{projectNameParam}");
		Assert.False (fileSystem.File.Exists (fullProjectPath));
		expectedError = expectedError
			.Replace ("{Directory}", File.Exists (fullProjectPath) ? Path.GetDirectoryName (fullProjectPath) : fullProjectPath)
			.Replace ("{CsProjFile}", fullProjectPath);


		var tfm = "net8.0-macos";
		var targetPath = "";
		var baseCommand = new BaseCommand<string> (fileSystem, "test", "test description", fullProjectPath, tfm, targetPath);

		var validation = baseCommand.ValidateCommand (fullProjectPath, tfm, targetPath);

		Assert.Contains (expectedError, validation.Error);
	}

	[Theory]
	[InlineData ("{Directory}/xcode", false, "The target path '{TargetPath}' does not exist. Use [--force, -f] to force creation.")]
	[InlineData ("{Directory}/xcode", true, "")]
	public void TestXcodeCommandValidation (string targetPath, bool force, string expectedError)
	{
		var fileSystem = new MockFileSystem ();

		var projectName = Guid.NewGuid ().ToString ();
		var tmpDir = Cache.CreateTemporaryDirectory (projectName);
		Assert.True (Directory.Exists (tmpDir));

		// Run 'dotnet new macos' in temp dir
		// DotnetNew (TestOutput, "globaljson", tmpDir, "--sdk-version 8.0.0 --roll-forward feature");
		DotnetNew (TestOutput, "macos", tmpDir);

		string fullProjectPath = $"{tmpDir}{projectName}/{projectName}.csproj";
		string tfm = "net8.0-macos";

		targetPath = targetPath
			.Replace ("{Directory}", Path.GetDirectoryName (fullProjectPath));

		expectedError = expectedError
			.Replace ("{Directory}", Path.GetDirectoryName (fullProjectPath))
			.Replace ("{CsProjFile}", fullProjectPath)
			.Replace ("{TargetFramework}", tfm)
			.Replace ("{TargetPath}", targetPath);

		var XcodeCommand = new XcodeCommand<string> (fileSystem, "test", "test description", fullProjectPath, tfm, targetPath, force);

		var validation = XcodeCommand.ValidateCommand (fullProjectPath, tfm, targetPath);

		var errorMessage = XcodeCommand.ValidateCommand (force);

		Assert.Equal (expectedError, errorMessage);
		if (string.IsNullOrEmpty (expectedError)) {
			Assert.NotEmpty (validation.ProjectPath);
			Assert.EndsWith (".csproj", validation.ProjectPath);
			Assert.NotEmpty (validation.Tfm);
			Assert.NotEmpty (validation.TargetPath);
			Assert.True (fileSystem.Directory.Exists (validation.TargetPath));
		}
	}

	[Theory]
	[InlineData (net_8_0_macosProject, new string [] { "net8.0-macos" })]
	[InlineData (mauiProject, new string [] { "net8.0-android", "net8.0-ios", "net8.0-maccatalyst", "net8.0-windows10.0.19041.0" })]
	public void BaseCommandValidation_GetTFMs (string csProject, string [] expectedTfms)
	{
		var projectName = Guid.NewGuid ().ToString ();
		var projectDir = "/src";

		var fileSystem = new MockFileSystem (
			new Dictionary<string, MockFileData> {
				{ Path.Combine (projectDir, $"{projectName}.csproj"), new MockFileData (csProject) },
			}
		);

		var baseCommand = new BaseCommand<string> (fileSystem, "test", "test description");

		baseCommand.TryGetTfmFromProject (Path.Combine (projectDir, $"{projectName}.csproj"), out var tfms);

		Assert.NotNull (tfms);
		Assert.NotEmpty (tfms);
		Assert.Equivalent (expectedTfms, tfms);
	}

	const string net_8_0_iosProject = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
	<TargetFramework>net8.0-ios</TargetFramework>
	<OutputType>Exe</OutputType>
	<Nullable>enable</Nullable>
	<ImplicitUsings>true</ImplicitUsings>
	<SupportedOSPlatformVersion>13.0</SupportedOSPlatformVersion>
  </PropertyGroup>
</Project>
";
	const string net_8_0_macosProject = @"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>net8.0-macos</TargetFramework>
        <RootNamespace>test_project</RootNamespace>
        <OutputType>Exe</OutputType>
        <Nullable>enable</Nullable>
        <ImplicitUsings>true</ImplicitUsings>
        <SupportedOSPlatformVersion>10.15</SupportedOSPlatformVersion>
    </PropertyGroup>
</Project>
";
	const string mauiProject = @"
<Project Sdk=""Microsoft.NET.Sdk"">

	<PropertyGroup>
		<TargetFrameworks>net8.0-android;net8.0-ios;net8.0-maccatalyst</TargetFrameworks>
		<TargetFrameworks Condition=""$([MSBuild]::IsOSPlatform('windows'))"">$(TargetFrameworks);net8.0-windows10.0.19041.0</TargetFrameworks>
		<!-- Uncomment to also build the tizen app. You will need to install tizen by following this: https://github.com/Samsung/Tizen.NET -->
		<!-- <TargetFrameworks>$(TargetFrameworks);net8.0-tizen</TargetFrameworks> -->

		<!-- Note for MacCatalyst:
		The default runtime is maccatalyst-x64, except in Release config, in which case the default is maccatalyst-x64;maccatalyst-arm64.
		When specifying both architectures, use the plural <RuntimeIdentifiers> instead of the singular <RuntimeIdentifier>.
		The Mac App Store will NOT accept apps with ONLY maccatalyst-arm64 indicated;
		either BOTH runtimes must be indicated or ONLY macatalyst-x64. -->
		<!-- For example: <RuntimeIdentifiers>maccatalyst-x64;maccatalyst-arm64</RuntimeIdentifiers> -->

		<OutputType>Exe</OutputType>
		<RootNamespace>MauiTest</RootNamespace>
		<UseMaui>true</UseMaui>
		<SingleProject>true</SingleProject>
		<ImplicitUsings>enable</ImplicitUsings>
		<Nullable>enable</Nullable>

		<!-- Display name -->
		<ApplicationTitle>MauiTest</ApplicationTitle>

		<!-- App Identifier -->
		<ApplicationId>com.companyname.mauitest</ApplicationId>

		<!-- Versions -->
		<ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
		<ApplicationVersion>1</ApplicationVersion>

		<SupportedOSPlatformVersion Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'"">11.0</SupportedOSPlatformVersion>

		<SupportedOSPlatformVersion Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'maccatalyst'"">13.1</SupportedOSPlatformVersion>
		<SupportedOSPlatformVersion Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'"">21.0</SupportedOSPlatformVersion>
		<SupportedOSPlatformVersion Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'"">10.0.17763.0</SupportedOSPlatformVersion>
		<TargetPlatformMinVersion Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'"">10.0.17763.0</TargetPlatformMinVersion>
		<SupportedOSPlatformVersion Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'tizen'"">6.5</SupportedOSPlatformVersion>
	</PropertyGroup>

	<ItemGroup>
		<!-- App Icon -->
		<MauiIcon Include=""Resources\AppIcon\appicon.svg"" ForegroundFile=""Resources\AppIcon\appiconfg.svg"" Color=""#512BD4"" />

		<!-- Splash Screen -->
		<MauiSplashScreen Include=""Resources\Splash\splash.svg"" Color=""#512BD4"" BaseSize=""128,128"" />

		<!-- Images -->
		<MauiImage Include=""Resources\Images\*"" />
		<MauiImage Update=""Resources\Images\dotnet_bot.png"" Resize=""True"" BaseSize=""300,185"" />

		<!-- Custom Fonts -->
		<MauiFont Include=""Resources\Fonts\*"" />

		<!-- Raw Assets (also remove the ""Resources\Raw"" prefix) -->
		<MauiAsset Include=""Resources\Raw\**"" LogicalName=""%(RecursiveDir)%(Filename)%(Extension)"" />
	</ItemGroup>

	<ItemGroup>
		<PackageReference Include=""Microsoft.Maui.Controls"" Version=""$(MauiVersion)"" />
		<PackageReference Include=""Microsoft.Maui.Controls.Compatibility"" Version=""$(MauiVersion)"" />
		<PackageReference Include=""Microsoft.Extensions.Logging.Debug"" Version=""9.0.0-preview.1.24080.9"" />
	</ItemGroup>

</Project>
";


}
