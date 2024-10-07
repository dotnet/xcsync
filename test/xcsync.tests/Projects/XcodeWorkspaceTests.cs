// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Moq;
using Serilog;
using Xamarin;
using xcsync.Projects;
using xcsync.Workers;
using Xunit.Abstractions;

namespace xcsync.tests.Projects;

public partial class XcodeWorkspaceTests (ITestOutputHelper TestOutput) : Base {

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
		var typeService = new TypeService (testLogger);

		var projectName = Guid.NewGuid ().ToString ();

		var tmpDir = Cache.CreateTemporaryDirectory (projectName);

		await DotnetNew (TestOutput, projectType, tmpDir, string.Empty);
		var newProject = projectType;

		var xcodeDir = Path.Combine (tmpDir, "obj", "xcode");
		Directory.CreateDirectory (xcodeDir);

		var csproj = Path.Combine (tmpDir, $"{projectName}.csproj");

		// Run 'xcsync generate'
		await new SyncContext (new FileSystem (), new TypeService (testLogger), SyncDirection.ToXcode, csproj, xcodeDir, tfm, testLogger).SyncAsync ();

		var dotNetProject = new ClrProject (fileSystem, testLogger, typeService, projectName, csproj, tfm);
		await dotNetProject.OpenProject ();

		var xcodeWorkspace = new XcodeWorkspace (fileSystem, testLogger, typeService, projectName, xcodeDir, tfm);

		await xcodeWorkspace.LoadAsync ();

		// Act
		await xcodeWorkspace.Items
			.Where (item => item is SyncableType)
			.Select (item => item as SyncableType)
			.Where (item => string.CompareOrdinal (Path.GetFileName (item!.FilePath), fileToParse) == 0)
			.Select (item => xcodeWorkspace.LoadTypesFromObjCFileAsync (item!.FilePath, visitor))
			.First ();

		// Assert
		Assert.Equal (expectedTypes, visitor.ObjCTypes.Select (t => t.Name).ToArray ());
	}

	[Theory]
	[InlineData ("macos", "net8.0-macos", EmptyViewControllerObjC_H, EmptyViewControllerObjC_M, EmptyViewController_CS)]
	public async Task UpdateRoslynType_ReturnsExpectedTypeChanges (string projectType, string tfm, string objCHeader, string objCModule, string expectedType)
	{

		// Arrange
		var fileSystem = new FileSystem ();

		var projectName = Guid.NewGuid ().ToString ();

		var tmpDir = Cache.CreateTemporaryDirectory (projectName);

		expectedType = expectedType
			.Replace ("##NAMESPACE##", CreateValidIdentifier (projectName));

		await DotnetNew (TestOutput, projectType, tmpDir, string.Empty);

		var xcodeDir = Path.Combine (tmpDir, "obj", "xcode");
		Directory.CreateDirectory (xcodeDir);

		var csproj = Path.Combine (tmpDir, $"{projectName}.csproj");

		// Run 'xcsync generate'
		await new SyncContext (new FileSystem (), new TypeService (testLogger), SyncDirection.ToXcode, csproj, xcodeDir, tfm, testLogger).SyncAsync ();

		// Update the ObjC header and module
		var headerPath = Path.Combine (xcodeDir, "ViewController.h");
		var modulePath = Path.Combine (xcodeDir, "ViewController.m");
		File.WriteAllText (headerPath, objCHeader);
		File.WriteAllText (modulePath, objCModule);

		var typeService = new TypeService (testLogger);

		var dotNetProject = new ClrProject (fileSystem, testLogger, typeService, projectName, csproj, tfm);
		await dotNetProject.OpenProject ();

		var xcodeWorkspace = new XcodeWorkspace (fileSystem, testLogger, typeService, projectName, xcodeDir, tfm);

		await xcodeWorkspace.LoadAsync ();

		var loader = new ObjCTypesLoader (testLogger);

		// Act
		List<Task> tasks = [];
		foreach (var syncItem in xcodeWorkspace.Items) {
			TaskCompletionSource? completionSource = null;
			Task task = syncItem switch {
				SyncableType type => loader.ConsumeAsync (new LoadTypesFromObjCMessage (Guid.NewGuid ().ToString (), completionSource = new (), xcodeWorkspace, syncItem), CancellationToken.None),
				_ => Task.CompletedTask
			};
			if (completionSource is not null)
				tasks.Add (completionSource.Task);
			tasks.Add (task);
			await task;
		}
		Task.WaitAll ([.. tasks]);

		// Assert
		var typeSymbol = typeService.QueryTypes (null, "ViewController").First ()?.TypeSymbol!;
		SyntaxTree? syntaxTree = null;
		foreach (var syntaxRef in typeSymbol.DeclaringSyntaxReferences) {
			if (syntaxRef.SyntaxTree.FilePath.EndsWith ("designer.cs")) {
				syntaxTree = syntaxRef.SyntaxTree;
				break;
			}
		}
		Assert.NotNull (syntaxTree);

		var root = syntaxTree?.GetRoot ();
		var actualType = root?.ToString () ?? string.Empty;

		Assert.Equal (expectedType, actualType);
	}

	[Theory]
	[InlineData ("macos", "net8.0-macos")]
	public async Task SyncAsync_ToXcode_ReturnsExpectedTypeChanges (string projectType, string tfm)
	{

		// Arrange
		var fileSystem = new FileSystem ();

		var projectName = Guid.NewGuid ().ToString ();

		var tmpDir = Cache.CreateTemporaryDirectory (projectName);

		await DotnetNew (TestOutput, projectType, tmpDir, string.Empty);

		var xcodeDir = Path.Combine (tmpDir, "obj", "xcode");
		Directory.CreateDirectory (xcodeDir);

		var csproj = Path.Combine (tmpDir, $"{projectName}.csproj");

		// Run 'xcsync generate'
		await new SyncContext (new FileSystem (), new TypeService (testLogger), SyncDirection.ToXcode, csproj, xcodeDir, tfm, testLogger).SyncAsync ();
		// Run 'xcsync sync'
		await new SyncContext (new FileSystem (), new TypeService (testLogger), SyncDirection.FromXcode, csproj, xcodeDir, tfm, testLogger).SyncAsync ();
	}

	[Theory]
	[InlineData ("data/generated_pbxproj.in")]
	[InlineData ("data/converted_pbxproj.in")]
	public async void LoadAsync_FindsSDKRoot_WhenSDKRootIsInAnyReleaseConfiguration (string pbjProjFile)
	{
		// Arrange
		var pbxProjFileData = await File.ReadAllTextAsync (pbjProjFile);
		var fileSystem = new MockFileSystem ();
		var projectName = Guid.NewGuid ().ToString ();
		var xcodeDir = Path.Combine ("obj", "xcode");

		var projectPath = Path.Combine (xcodeDir, $"{projectName}.xcodeproj");

		var typeService = new TypeService (testLogger);

		fileSystem.Directory.CreateDirectory (projectPath);
		await fileSystem.File.WriteAllTextAsync (Path.Combine (projectPath, "project.pbxproj"), pbxProjFileData);

		var xcodeWorkspace = new XcodeWorkspace (fileSystem, testLogger, typeService, projectName, xcodeDir, "macos");

		// Act
		await xcodeWorkspace.LoadAsync ();

		// Assert
	}

	[Fact]
	public async void LoadAsync_WhenNoPbxProjFile_LogsErrorMessage ()
	{
		// Arrange
		var fileSystem = new MockFileSystem ();
		var logger = new Mock<ILogger> ();

		var projectName = Guid.NewGuid ().ToString ();
		var xcodeDir = Path.Combine ("obj", "xcode");

		var pbxProjFile = Path.Combine (xcodeDir, $"{projectName}.xcodeproj", "project.pbxproj");

		var xcodeWorkspace = new XcodeWorkspace (fileSystem, logger.Object, Mock.Of<ITypeService> (), projectName, xcodeDir, "macos");

		// Act
		await xcodeWorkspace.LoadAsync ();

		// Assert
		logger.Verify (l => l.Error (It.Is<string> (msg => string.CompareOrdinal (msg, Strings.XcodeWorkspace.XcodeProjectNotFound (pbxProjFile)) == 0)));
	}

	string CreateValidIdentifier (string projectName)
	{
		char [] numerals = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];

		if (Array.IndexOf (numerals, projectName [0]) > -1)
			projectName = $"_{projectName}";

		projectName = projectName.Replace ("-", "_");
		projectName = projectName.Replace (" ", "_");

		return NormalizeWhitespace (projectName);
	}

	[GeneratedRegex (@"\s")]
	private static partial Regex Normalize ();
	static string NormalizeWhitespace (string input) => Normalize ().Replace (input, "");

	const string EmptyViewControllerObjC_H = @"@interface ViewController : NSViewController {
}
 
@end	
";

	const string EmptyViewControllerObjC_M = @"@implementation ViewController
 
@end
";

	const string EmptyViewController_CS = @"namespace ##NAMESPACE##;

[Register(""ViewController"")]
partial class ViewController
{
    void ReleaseDesignerOutlets()
    {
    }
}";

}
