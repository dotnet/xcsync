// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections.ObjectModel;
using System.IO.Abstractions;
using ClangSharp;

// using ClangSharp;
using Moq;

using Serilog;
using Xamarin;


// using Xamarin;

using xcsync.Projects;

using Xunit.Abstractions;

namespace xcsync.tests.Projects;

public class XcodeWorkspaceTests (ITestOutputHelper TestOutput) : Base {

	readonly ILogger testLogger = new LoggerConfiguration ()
		.MinimumLevel.Verbose ()
		.WriteTo.TestOutput (TestOutput)
		.CreateLogger ();

	[Theory]
	[InlineData ("macos", "net8.0-macos", "ViewController.m", new [] { "ViewController" })]
	[InlineData ("maccatalyst", "net8.0-maccatalyst", "SceneDelegate.m", new [] { "SceneDelegate" })]
	[InlineData ("ios", "net8.0-ios", "SceneDelegate.m", new [] { "SceneDelegate" })]
	[InlineData ("tvos", "net8.0-tvos", "ViewController.m", new [] { "ViewController" })]
	[InlineData ("maui", "net8.0-ios", "AppDelegate.m", new [] { "AppDelegate" })]
	[InlineData ("maui", "net8.0-maccatalyst", "AppDelegate.m", new [] { "AppDelegate" })]
	public async Task LoadObjcHeaderTypes_ShouldReturnTypes (string projectType, string tfm, string fileToParse, string [] expectedTypes)
	{
		// Arrange
		var fileSystem = new FileSystem ();
		var visitor = new ObjCImplementationDeclVisitor (testLogger);

		var projectName = Guid.NewGuid ().ToString ();

		var tmpDir = Cache.CreateTemporaryDirectory (projectName);

		await DotnetNew (TestOutput, projectType, tmpDir, string.Empty);
		var newProject = projectType;

		var xcodeDir = Path.Combine (tmpDir, "obj", "xcode");
		Directory.CreateDirectory (xcodeDir);

		var csproj = Path.Combine (tmpDir, $"{projectName}.csproj");

		// Run 'xcsync generate'
		await Xcsync (TestOutput, "generate", "--project", csproj, "--target", xcodeDir, "-tfm", tfm);

		var xcodeWorkspace = new XcodeWorkspace (fileSystem, testLogger, projectName, xcodeDir, tfm);

		// Act
		await xcodeWorkspace.LoadObjCTypesFromFilesAsync ([Path.Combine (xcodeDir, fileToParse)], visitor);

		// Assert
		Assert.Equal (expectedTypes, visitor.ObjCTypes.Select (t => t.Name).ToArray ());
	}
}
