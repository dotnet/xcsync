// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Moq;
using Serilog;
using Xamarin;
using xcsync.Commands;
using Xunit.Abstractions;

namespace xcsync.tests.Commands;

public class CommandValidationTests (ITestOutputHelper TestOutput) : Base {
	[Fact (Skip = "https://github.com/dotnet/xcsync/issues/132")]
	public void TestXcSyncCommandCreation ()
	{
		var fileSystem = new MockFileSystem ();

		var command = new XcSyncCommand (fileSystem, Mock.Of<ILogger> ());
		Assert.NotNull (command);
		command.Invoke (["--dotnet-path", "/path/to/dotnet"], new CapturingConsole ());
		Assert.Equal ("/path/to/dotnet", xcSync.DotnetPath);
	}

	[Fact]
	public void TestBaseCommandCreation ()
	{
		var fileSystem = new MockFileSystem ();
		var logger = new XunitLogger (TestOutput);
		var command = new BaseCommand<string> (fileSystem, logger, "test", "test description");
		Assert.NotNull (command);
	}

	[Fact]
	public void TestXcodeCommandCreation ()
	{
		var fileSystem = new MockFileSystem ();
		var logger = new XunitLogger (TestOutput);

		var command = new XcodeCommand<string> (fileSystem, logger, "test", "test description");
		Assert.NotNull (command);
	}

	[Fact]
	public async Task GenerateCommand_WhenNoTargetSpecified_UsesDefaultTargetDirectory ()
	{
		var projectName = Guid.NewGuid ().ToString ();
		var tmpDir = Cache.CreateTemporaryDirectory (projectName);

		await DotnetNew (TestOutput, "ios", tmpDir);
		var csproj = Path.Combine (tmpDir, $"{projectName}.csproj");
		Assert.True (File.Exists (csproj));

		var intermediateOutputPath = Scripts.GetIntermediateOutputPath (csproj, "net8.0-ios");
		var fileSystem = new FileSystem ();
		var logger = new XunitLogger (TestOutput);
		var command = new GenerateCommand (fileSystem, logger);
		int exitCode = command.Invoke ($"--project {csproj} -f");

		// ensure the default target directory is created relative to the project directory, not the pwd
		Assert.Equal (0, exitCode);
		Assert.True (Directory.Exists (Path.Combine (tmpDir, intermediateOutputPath, "xcode")));
	}

	[Theory]
	[InlineData ("macos", "", "", "")]
	[InlineData ("macos", "net8.0-macos", "", "")]
	[InlineData ("macos", "net8.0-macos", "obj/xcode", "")]
	[InlineData ("macos", "net9.0-macos", "obj/xcode", "Target framework is not supported by current .NET project.")]
	[InlineData ("maui", "", "", "Multiple target frameworks found in the project file. Specify which target framework to use with the [--target-framework, -tfm] option.")]
	[InlineData ("maui", "net8.0-ios", "obj/xcode", "")]
	[InlineData ("maui", "net8.0-maccatalyst", "obj/xcode", "")]
	[InlineData ("maui", "net8.0-macos", "obj/xcode", "Target framework is not supported by current .NET project.")]
	[InlineData ("maui", "net8.0-ios", "{Directory}/xcode", "Target path '{TargetPath}' does not exist, will create directory if [--force, -f] is set.")]
	public async void BaseCommandValidation_SingleProject (string projectType, string tfm, string targetPath, string expectedError)
	{
		var projectName = Guid.NewGuid ().ToString ();
		var tmpDir = Cache.CreateTemporaryDirectory (projectName);

		var fileSystem = new FileSystem ();
		var logger = new XunitLogger (TestOutput);

		await DotnetNew (TestOutput, projectType, tmpDir, "");
		Assert.True (Directory.Exists (tmpDir));
		var fullProjectPath = Path.Combine (tmpDir, $"{projectName}.csproj");

		targetPath = targetPath
			.Replace ("{Directory}", Path.GetDirectoryName (fullProjectPath));

		expectedError = expectedError
			.Replace ("{Directory}", fileSystem.File.Exists (fullProjectPath) ? fileSystem.Path.GetDirectoryName (fullProjectPath) : fullProjectPath)
			.Replace ("{CsProjFile}", fullProjectPath)
			.Replace ("{TargetFramework}", tfm)
			.Replace ("{TargetPath}", targetPath);

		var baseCommand = new BaseCommand<string> (fileSystem, logger, "test", "test description");

		var args = new string [] {
			"--project", fullProjectPath,
			"--target-framework-moniker", tfm,
			"--target", targetPath
		 };

		var console = new CapturingConsole ();
		int exitCode = baseCommand.Invoke (args, console);

		var errorMessage = console.ErrorOutput.Count > 0 ? console.ErrorOutput [0] : string.Empty;

		Assert.Equal (expectedError, errorMessage);
		if (string.IsNullOrEmpty (expectedError)) {
			Assert.Equal (0, exitCode);
		}
	}

	[Theory]
	[InlineData ("/src", "", "Multiple .csproj files found in '{CsProjFile}', please specify which project file to use via the [--project, -p] option:")]
	[InlineData ("", ".", "Multiple .csproj files found in '{CsProjFile}', please specify which project file to use via the [--project, -p] option:")]
	[InlineData ("", "does-not-exist.csproj", "Path '{CsProjFile}' does not exist")]
	[InlineData ("/src", "does-not-exist.csproj", "Path '{CsProjFile}' does not exist")]
	public void BaseCommandValidation_MultipleProjects (string projectDir, string projectNameParam, string expectedError)
	{
		var projectName = Guid.NewGuid ().ToString ();

		var fileSystem = new MockFileSystem (
			new Dictionary<string, MockFileData> {
				{ Path.Combine (projectDir, $"{projectName}.csproj"), new MockFileData ("") },
				{ Path.Combine (projectDir, $"{projectName}-2.csproj"), new MockFileData ("") }
			}
		);
		var logger = new XunitLogger (TestOutput);

		var fullProjectPath = fileSystem.Path.Combine (projectDir, $"{projectNameParam}");
		Assert.False (fileSystem.File.Exists (fullProjectPath));
		expectedError = expectedError
			.Replace ("{Directory}", File.Exists (fullProjectPath) ? Path.GetDirectoryName (fullProjectPath) : fullProjectPath)
			.Replace ("{CsProjFile}", fullProjectPath);


		var tfm = "net8.0-macos";
		var targetPath = "";
		var baseCommand = new BaseCommand<string> (fileSystem, logger, "test", "test description");

		var args = new string [] {
			"--project", fullProjectPath,
			"--target-framework-moniker", tfm,
			"--target", targetPath
		 };

		var console = new CapturingConsole ();
		var exitCode = baseCommand.Invoke (args, console);

		var errorMessage = console.ErrorOutput.Count > 0 ? console.ErrorOutput [0] : string.Empty;

		Assert.StartsWith (expectedError, errorMessage);
		if (string.IsNullOrEmpty (expectedError)) {
			Assert.Equal (0, exitCode);
		}
	}

	[Theory]
	[InlineData ("{Directory}/xcode", false, "Target path '{TargetPath}' does not exist, will create directory if [--force, -f] is set.")]
	[InlineData ("{Directory}/xcode", true, "")]
	[InlineData ("{Directory}/some/rando/folder/xcode", false, "Target path '{TargetPath}' does not exist, will create directory if [--force, -f] is set.")]
	[InlineData ("{Directory}/some/rando/folder/xcode", true, "")]
	public async void TestXcodeCommandValidation (string targetPath, bool force, string expectedError)
	{
		var fileSystem = new FileSystem ();
		var logger = new XunitLogger (TestOutput);

		var projectName = Guid.NewGuid ().ToString ();
		var tmpDir = Cache.CreateTemporaryDirectory (projectName);
		Assert.True (Directory.Exists (tmpDir));

		await DotnetNew (TestOutput, "macos", tmpDir);

		string fullProjectPath = $"{tmpDir}/{projectName}.csproj";
		string tfm = "net8.0-macos";

		targetPath = targetPath
			.Replace ("{Directory}", Path.GetDirectoryName (fullProjectPath));

		expectedError = expectedError
			.Replace ("{Directory}", Path.GetDirectoryName (fullProjectPath))
			.Replace ("{CsProjFile}", fullProjectPath)
			.Replace ("{TargetFramework}", tfm)
			.Replace ("{TargetPath}", targetPath);

		var XcodeCommand = new XcodeCommand<string> (fileSystem, logger, "test", "test description");

		var args = new string [] {
			"--project", fullProjectPath,
			"--target-framework-moniker", tfm,
			"--target", targetPath
		 };
		if (force) {
			args = [.. args, "--force"];
		}
		var console = new CapturingConsole ();
		int exitCode = XcodeCommand.Invoke (args, console);

		var errorMessage = console.ErrorOutput.Count > 0 ? console.ErrorOutput [0] : string.Empty;

		Assert.Equal (expectedError, errorMessage);
		if (string.IsNullOrEmpty (expectedError)) {
			Assert.Equal (0, exitCode);
		}
	}

	[Theory]
	[InlineData ("macos", new string [] { "net8.0-macos" })]
	[InlineData ("maui", new string [] { "net8.0-android", "net8.0-ios", "net8.0-maccatalyst" })]
	public async void BaseCommandValidation_GetTFMs (string projectType, string [] expectedTfms)
	{
		var projectName = Guid.NewGuid ().ToString ();

		var tmpDir = Cache.CreateTemporaryDirectory (projectName);

		var fileSystem = new FileSystem ();
		var logger = new XunitLogger (TestOutput);
		xcSync.Logger = logger;

		await DotnetNew (TestOutput, projectType, tmpDir, "");
		Assert.True (Directory.Exists (tmpDir));
		var fullProjectPath = fileSystem.Path.Combine (tmpDir, $"{projectName}.csproj");

		var baseCommand = new BaseCommand<string> (fileSystem, logger, "test", "test description");

		baseCommand.TryGetTfmFromProject (fullProjectPath, out var tfms);

		Assert.NotNull (tfms);
		Assert.NotEmpty (tfms);
		Assert.Equivalent (expectedTfms, tfms);
	}

	[WindowsOnlyFact]
	public async void BaseCommandValidation_WhenRunOnWindows_ReturnsError ()
	{
		var projectName = Guid.NewGuid ().ToString ();

		var tmpDir = Cache.CreateTemporaryDirectory (projectName);

		var fileSystem = new FileSystem ();
		var logger = new XunitLogger (TestOutput);
		xcSync.Logger = logger;

		await DotnetNew (TestOutput, "ios", tmpDir, "");
		Assert.True (Directory.Exists (tmpDir));
		var fullProjectPath = fileSystem.Path.Combine (tmpDir, $"{projectName}.csproj");

		var baseCommand = new BaseCommand<string> (fileSystem, logger, "test", "test description");
		var args = new string [] {
			"--project", fullProjectPath,
			"--target-framework-moniker", "net8.0-ios",
			"--target", "obj/xcode"
		 };

		var console = new CapturingConsole ();
		int exitCode = baseCommand.Invoke (args, console);

		var errorMessage = console.ErrorOutput.Count > 0 ? console.ErrorOutput [0] : string.Empty;

		Assert.Equal ("The xcsync tool can only be run on macOS.", errorMessage);
		Assert.Equal (1, exitCode);
	}

	[MacOSOnlyFact]
	public async void BaseCommandValidation_WhenRunOnMacOS_DoesNotReturnError ()
	{
		var projectName = Guid.NewGuid ().ToString ();

		var tmpDir = Cache.CreateTemporaryDirectory (projectName);

		var fileSystem = new FileSystem ();
		var logger = new XunitLogger (TestOutput);
		xcSync.Logger = logger;

		await DotnetNew (TestOutput, "ios", tmpDir, "");
		Assert.True (Directory.Exists (tmpDir));
		var fullProjectPath = fileSystem.Path.Combine (tmpDir, $"{projectName}.csproj");

		var intermediateOutputPath = Scripts.GetIntermediateOutputPath (fullProjectPath, "net8.0-ios");

		var baseCommand = new BaseCommand<string> (fileSystem, logger, "test", "test description");
		var args = new string [] {
			"--project", fullProjectPath,
			"--target-framework-moniker", "net8.0-ios",
			"--target", $"{intermediateOutputPath}/xcode"
		 };

		var console = new CapturingConsole ();
		int exitCode = baseCommand.Invoke (args, console);

		var errorMessage = console.ErrorOutput.Count > 0 ? console.ErrorOutput [0] : string.Empty;

		Assert.Equal ("", errorMessage);
		Assert.Equal (0, exitCode);
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
