// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Serilog;
using Xamarin;
using Xunit.Abstractions;

namespace xcsync.tests;

public class ScriptsTests (ITestOutputHelper TestOutput) : Base {

	readonly ILogger testLogger = new LoggerConfiguration ()
		.MinimumLevel.Verbose ()
		.WriteTo.TestOutput (TestOutput)
		.CreateLogger ();

	[Fact]
	public void SelectXcode_ReturnsXcodePath ()
	{
		// Act
		string xcodePath = Scripts.SelectXcode ();

		// Assert
		Assert.NotNull (xcodePath);
		Assert.NotEmpty (xcodePath);
		Assert.EndsWith (".app", xcodePath);
	}

	[Fact]
	public void IsMauiAppProject_TestProject_DoesNotFail ()
	{
		// Arrange

		// Act
		bool isMauiAppProject = Scripts.IsMauiAppProject (TestProjectPath);

		// Assert
		Assert.False (isMauiAppProject);
	}

	[Theory]
	[InlineData ("ios", false)]
	[InlineData ("macos", false)]
	[InlineData ("tvos", false)]
	[InlineData ("maccatalyst", false)]
	[InlineData ("maui", true)]
	[InlineData ("mauilib", false)]
	public async Task IsMauiAppProject (string projectType, bool expected)
	{
		// Arrange
		var projectName = Guid.NewGuid ().ToString ();
		var tmpDir = Cache.CreateTemporaryDirectory (projectName);
		var csproj = Path.Combine (tmpDir, $"{projectName}.csproj");

		// Run 'dotnet new macos' in temp dir
		await DotnetNew (TestOutput, projectType, tmpDir, string.Empty);

		Assert.True (Directory.Exists (tmpDir));

		// Act
		bool isMauiAppProject = Scripts.IsMauiAppProject (csproj);

		// Assert
		Assert.Equal (expected, isMauiAppProject);
	}

	[Theory]
	[InlineData ("ios", "net8.0-ios;net9.0-ios")]
	[InlineData ("macos", "net8.0-macos;net9.0-macos")]
	[InlineData ("maui", "net8.0-ios;net8.0-maccatalyst;net8.0-android")]
	[InlineData ("mauilib", "net8.0-ios;net8.0-maccatalyst;net8.0-android")]
	public async Task GetTargetFrameworksFromProject_ReturnsExpectedFrameworks (string projectType, string frameworks)
	{
		// Arrange
		var projectName = Guid.NewGuid ().ToString ();
		var tmpDir = Cache.CreateTemporaryDirectory (projectName);
		var csproj = Path.Combine (tmpDir, $"{projectName}.csproj");

		// Run 'dotnet new macos' in temp dir
		await DotnetNew (TestOutput, projectType, tmpDir, string.Empty);

		SetTargetFrameworks (csproj, frameworks.Split (';'));

		var expected = frameworks.Split (';');

		// Act
		var projectFrameworks = Scripts.GetTargetFrameworksFromProject (new FileSystem (), csproj);

		// Assert
		Assert.Equal (expected, projectFrameworks);
	}

	[Fact]
	public async Task GetTargetFrameworksFromProject_NoFrameworks_ReturnsEmpty ()
	{
		// Arrange
		var projectName = Guid.NewGuid ().ToString ();
		var tmpDir = Cache.CreateTemporaryDirectory (projectName);
		var csproj = Path.Combine (tmpDir, $"{projectName}.csproj");

		// Run 'dotnet new macos' in temp dir
		await DotnetNew (TestOutput, "classlib", tmpDir, string.Empty);

		SetTargetFrameworks (csproj, []);

		// Act
		var frameworks = Scripts.GetTargetFrameworksFromProject (new FileSystem (), csproj);

		// Assert
		Assert.NotNull (frameworks);
		Assert.Empty (frameworks);
	}

	static void SetTargetFrameworks (string csproj, string [] frameworks)
	{
		var doc = System.Xml.Linq.XDocument.Load (csproj);
		var projectElement = doc.Element ("Project");
		if (projectElement != null) {
			var targetFrameworkElement = projectElement.Element ("PropertyGroup")?.Element ("TargetFramework");
			if (targetFrameworkElement != null) {
				targetFrameworkElement.Name = "TargetFrameworks";
			}
			var targetFrameworksElement = projectElement.Element ("PropertyGroup")?.Element ("TargetFrameworks");
			if (targetFrameworksElement != null) {
				targetFrameworksElement.Value = string.Join (";", frameworks);
				doc.Save (csproj);
			}
		}
	}

	[Fact]
	public void GetTargetFrameworksFromProject_InvalidPath_ThrowsException ()
	{
		// Arrange
		var csproj = Path.Combine ("path/to/unknown/project.csproj");

		// Act & Assert
		Assert.Throws<InvalidOperationException> (() => Scripts.GetTargetFrameworksFromProject (new FileSystem (), csproj));
	}
}
