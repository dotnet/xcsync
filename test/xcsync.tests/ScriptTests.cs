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
}
