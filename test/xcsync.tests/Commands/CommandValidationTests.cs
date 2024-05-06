// Copyright (c) Microsoft Corporation.  All rights reserved.

using Xamarin;
using xcsync.Commands;
using Xunit.Abstractions;

namespace xcsync.tests.Commands;

public class CommandValidationTests (ITestOutputHelper TestOutput) : Base {

	[Fact]
	public void TestBaseCommandCreation ()
	{
		var baseCommand = new BaseCommand<string> ("test", "test description");
		Assert.NotNull (baseCommand);
	}

	[Fact]
	public void TestXcodeCommandCreation ()
	{
		var baseCommand = new XcodeCommand<string> ("test", "test description");
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
		var projectName = Guid.NewGuid ().ToString ();
		var tmpDir = Cache.CreateTemporaryDirectory (projectName);

		// Run 'dotnet new macos' in temp dir
		// DotnetNew (TestOutput, "globaljson", tmpDir, "--sdk-version 8.0.0 --roll-forward feature");
		DotnetNew (TestOutput, projectType, tmpDir);

		Assert.True (Directory.Exists (tmpDir));

		var fullProjectPath = Path.Combine (tmpDir, $"{projectName}.csproj");
		Assert.True (File.Exists (fullProjectPath));

		targetPath = targetPath
			.Replace ("{Directory}", Path.GetDirectoryName (fullProjectPath));

		expectedError = expectedError
			.Replace ("{Directory}", File.Exists (fullProjectPath) ? Path.GetDirectoryName (fullProjectPath) : fullProjectPath)
			.Replace ("{CsProjFile}", fullProjectPath)
			.Replace ("{TargetFramework}", tfm)
			.Replace ("{TargetPath}", targetPath);

		var baseCommand = new BaseCommand<string> ("test", "test description", fullProjectPath, tfm, targetPath);

		var errorMessage = baseCommand.ValidateCommand (ref fullProjectPath, ref tfm, ref targetPath);

		Assert.Equal (expectedError, errorMessage);
		if (string.IsNullOrEmpty (expectedError)) {
			Assert.NotEmpty (fullProjectPath);
			Assert.EndsWith (".csproj", fullProjectPath);
			Assert.NotEmpty (tfm);
			Assert.NotEmpty (targetPath);
			Assert.True (Directory.Exists (targetPath));
		}
	}

	[Theory]
	[InlineData ("", "Multiple .csproj files found in")]
	[InlineData ("does-not-exist.csproj", "File not found")]
	public void BaseCommandValidation_MultipleProjects (string projectNameParam, string expectedError)
	{
		var projectName = Guid.NewGuid ().ToString ();
		var tmpDir = Cache.CreateTemporaryDirectory (projectName);
		Assert.True (Directory.Exists (tmpDir));

		// Run 'dotnet new macos' in temp dir
		// DotnetNew (TestOutput, "globaljson", tmpDir, "--sdk-version 8.0.0 --roll-forward feature");
		DotnetNew (TestOutput, "macos", tmpDir);

		File.Copy (Path.Combine (tmpDir, $"{projectName}.csproj"), Path.Combine (tmpDir, $"{projectName}-2.csproj"));

		var fullProjectPath = Path.Combine (tmpDir, $"{projectNameParam}");
		Assert.False (File.Exists (fullProjectPath));

		var tfm = "net8.0-macos"; var targetPath = "";
		var baseCommand = new BaseCommand<string> ("test", "test description", fullProjectPath, tfm, targetPath);

		var errorMessage = baseCommand.ValidateCommand (ref fullProjectPath, ref tfm, ref targetPath);

		Assert.Contains (expectedError, errorMessage);
	}

	[Theory]
	[InlineData ("{Directory}/xcode", false, "The target path '{TargetPath}' does not exist. Use [--force, -f] to force creation.")]
	[InlineData ("{Directory}/xcode", true, "")]
	public void TestXcodeCommandValidation (string targetPath, bool force, string expectedError)
	{
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

		var XcodeCommand = new XcodeCommand<string> ("test", "test description", fullProjectPath, tfm, targetPath, force);

		var errorMessage = XcodeCommand.ValidateCommand (ref fullProjectPath, ref tfm, ref targetPath);

		errorMessage = XcodeCommand.ValidateCommand (force);

		Assert.Equal (expectedError, errorMessage);
		if (string.IsNullOrEmpty (expectedError)) {
			Assert.NotEmpty (fullProjectPath);
			Assert.EndsWith (".csproj", fullProjectPath);
			Assert.NotEmpty (tfm);
			Assert.NotEmpty (targetPath);
			Assert.True (Directory.Exists (targetPath));
		}
	}

}
