// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xamarin;
using Xunit.Abstractions;

namespace xcsync.e2e.tests.UseCases;

public partial class GenerateThenSyncWithChangesTests (ITestOutputHelper testOutput) : Base (testOutput) {
	public static IEnumerable<object []> AddControlAndOutlet =>
	[
		["macos", "net8.0-macos", (ITestOutputHelper testOutput, string path, string projectType, string tfm) => AddControlAndOutletChanges (testOutput, path, projectType, tfm)],
		["macos", "net8.0-macos", (ITestOutputHelper testOutput, string path, string projectType, string tfm) => AddControlAndOutletChangesFromDiff (testOutput, "data/add_outlet_using_xcode.diff", path, projectType, tfm)],
		// ["maccatalyst", "net8.0-maccatalyst", (string path, string projectType, string tfm) => AddControlAndOutletChanges (path, projectType, tfm)], 
		// ["ios", "net8.0-ios", (string path, string projectType, string tfm) => AddControlAndOutletChanges (path, projectType, tfm)], # No ViewController.cs file
		["tvos", "net8.0-tvos", (ITestOutputHelper testOutput, string path, string projectType, string tfm) => AddControlAndOutletChanges (testOutput, path, projectType, tfm)],
		// ["maui", "net8.0-ios", (string path, string projectType, string tfm) => AddControlAndOutletChanges (path, projectType, tfm)], # No ViewController.cs file
		// ["maui", "net8.0-maccatalyst", (string path, string projectType, string tfm) => AddControlAndOutletChanges (path, projectType, tfm)] # No ViewController.cs file
	];

	public static IEnumerable<object []> AddXcodeDotnetChanges =>
	[
		["macos", "net8.0-macos", (ITestOutputHelper testOutput, string path, string projectType, string tfm) => AddControlAndOutletChanges (testOutput, path, projectType, tfm), (ITestOutputHelper testOutput, string path, string projectType, string tfm, string projectName) => AddDotnetChanges (testOutput, path, projectType, tfm, projectName)],
		["tvos", "net8.0-tvos", (ITestOutputHelper testOutput, string path, string projectType, string tfm) => AddControlAndOutletChanges (testOutput, path, projectType, tfm), (ITestOutputHelper testOutput, string path, string projectType, string tfm, string projectName) => AddDotnetChanges (testOutput, path, projectType, tfm, projectName)],
	];

	[Theory]
	[MemberData (nameof (AddControlAndOutlet))]
	[Trait ("Category", "IntegrationTest")]
	public async Task GenerateThenSync_WithChanges_GeneratesChangesAsync (string projectType, string tfm, Func<ITestOutputHelper, string, string, string, Task> makeChanges)
	{
		// Arrange

		var projectName = Guid.NewGuid ().ToString ();

		var tmpDir = Cache.CreateTemporaryDirectory (projectName);

		var xcodeDir = Path.Combine (tmpDir, "obj", "xcode");

		Directory.CreateDirectory (xcodeDir);

		var csproj = Path.Combine (tmpDir, $"{projectName}.csproj");

		await Git (TestOutput, "init", tmpDir).ConfigureAwait (false);
		await DotnetNew (TestOutput, projectType, tmpDir, string.Empty).ConfigureAwait (false);
		await DotnetNew (TestOutput, "gitignore", tmpDir, string.Empty).ConfigureAwait (false);
		await Git (TestOutput, "-C", tmpDir, "add", ".").ConfigureAwait (false);
		await Git (TestOutput, "-C", tmpDir, "commit", "-m", "Initial commit").ConfigureAwait (false);

		// Act
		await Xcsync (TestOutput, "generate", "--project", csproj, "--target", xcodeDir, "-tfm", tfm).ConfigureAwait (false);
		await Git (TestOutput, "-C", tmpDir, "add", ".").ConfigureAwait (false);
		await Git (TestOutput, "-C", tmpDir, "commit", "-m", "Xcode Project Generation").ConfigureAwait (false);

		await makeChanges (TestOutput, xcodeDir, projectType, tfm).ConfigureAwait (false);
		await Git (TestOutput, "-C", tmpDir, "add", ".").ConfigureAwait (false);
		await Git (TestOutput, "-C", tmpDir, "commit", "-m", "Xcode Project Changes").ConfigureAwait (false);

		await Xcsync (TestOutput, "sync", "--project", csproj, "--target", xcodeDir, "-tfm", tfm).ConfigureAwait (false);

		// Assert
		var commandOutput = new CaptureOutput (TestOutput);
		var changesPresent = await Git (commandOutput, "-C", tmpDir, "diff-index", "--quiet", "HEAD", "--exit-code", "--").ConfigureAwait (false);
		if (changesPresent == 0)
			Assert.Fail ($"[{projectType},{tfm}] : Git diff-index failed, there are no changes in the source files.\n{commandOutput.Output}");
	}

	[Theory]
	[MemberData (nameof (AddXcodeDotnetChanges))]
	[Trait ("Category", "IntegrationTest")]
	public async Task GenerateAndSync_WithXcodeAndDotnetChanges_GeneratesChangesAsync (string projectType, string tfm, Func<ITestOutputHelper, string, string, string, Task> xcodeChanges, Func<ITestOutputHelper, string, string, string, string, Task> dotnetChanges)
	{
		// Arrange
		bool success = false;
		string failMessage = string.Empty;

		var projectName = Guid.NewGuid ().ToString ();

		var tmpDir = Cache.CreateTemporaryDirectory (projectName);

		var xcodeDir = Path.Combine (tmpDir, "obj", "xcode");

		Directory.CreateDirectory (xcodeDir);

		var csproj = Path.Combine (tmpDir, $"{projectName}.csproj");

		await Git (TestOutput, "init", tmpDir).ConfigureAwait (false);
		await DotnetNew (TestOutput, projectType, tmpDir, string.Empty).ConfigureAwait (false);
		await DotnetNew (TestOutput, "gitignore", tmpDir, string.Empty).ConfigureAwait (false);
		await Git (TestOutput, "-C", tmpDir, "add", ".").ConfigureAwait (false);
		await Git (TestOutput, "-C", tmpDir, "commit", "-m", "Initial commit").ConfigureAwait (false);

		// Act
		Assert.Equal (0, await Xcsync (TestOutput, "generate", "--project", csproj, "--target", xcodeDir, "-tfm", tfm).ConfigureAwait (false));
		// Assert that the changes are present in the .NET project source files
		(success, failMessage) = await CheckGitForChanges ($"{projectType},{tfm}", tmpDir);
		Assert.True (success, failMessage);

		await Git (TestOutput, "-C", tmpDir, "add", ".").ConfigureAwait (false);
		await Git (TestOutput, "-C", tmpDir, "commit", "-m", "Xcode Project Generation").ConfigureAwait (false);

		await xcodeChanges (TestOutput, xcodeDir, projectType, tfm).ConfigureAwait (false);
		await Git (TestOutput, "-C", tmpDir, "add", ".").ConfigureAwait (false);
		await Git (TestOutput, "-C", tmpDir, "commit", "-m", "Xcode Project Changes").ConfigureAwait (false);

		Assert.Equal (0, await Xcsync (TestOutput, "sync", "--project", csproj, "--target", xcodeDir, "-tfm", tfm).ConfigureAwait (false));
		// Assert that the changes are present in the .NET project source files
		(success, failMessage) = await CheckGitForChanges ($"{projectType},{tfm}", tmpDir);
		Assert.True (success, failMessage);

		await dotnetChanges (TestOutput, tmpDir, projectType, tfm, projectName).ConfigureAwait (false);
		await Git (TestOutput, "-C", tmpDir, "add", ".").ConfigureAwait (false);
		await Git (TestOutput, "-C", tmpDir, "commit", "-m", "Dotnet Project Changes").ConfigureAwait (false);

		Assert.Equal (0, await Xcsync (TestOutput, "generate", "--project", csproj, "--target", xcodeDir, "-tfm", tfm).ConfigureAwait (false));

		// Assert
		(success, failMessage) = await CheckGitForChanges ($"{projectType},{tfm}", tmpDir);
		Assert.True (success, failMessage);
	}

	async Task<(bool, string)> CheckGitForChanges (string label, string projectDir)
	{
		var failMessage = string.Empty;
		var commandOutput = new CaptureOutput (TestOutput);
		var changesPresent = await Git (commandOutput, "-C", projectDir, "diff-index", "--quiet", "HEAD", "--exit-code", "--").ConfigureAwait (false);
		if (changesPresent == 0)
			failMessage =  $"[{label}] : Git diff-index failed, there are no changes in the source files.\n{commandOutput.Output}";
		return (changesPresent == 0, failMessage);
	}

	static async Task AddDotnetChanges (ITestOutputHelper testOutput, string tmpDir, string projectType, string tfm, string projectName)
	{
		// change registered type name
		await File.WriteAllTextAsync (Path.Combine (tmpDir, "ViewController.cs"),
$@"namespace {projectName};

[Register (""NewDelegate"")]
public class AppDelegate : NSApplicationDelegate {{
    public override void DidFinishLaunching (NSNotification notification)
    {{
        // Insert code here to initialize your application
    }}

    public override void WillTerminate (NSNotification notification)
    {{
        // Insert code here to tear down your application
    }}
}}");

	}

	static async Task AddControlAndOutletChanges (ITestOutputHelper testOutput, string tmpDir, string projectType, string tfm)
	{
		await File.WriteAllTextAsync (Path.Combine (tmpDir, "ViewController.h"),
$@"// ------------------------------------------------------------------------------
// <auto-generated>
//     Code was generated by Microsoft (R) xcsync tool.
//     This file was generated automatically to mirror C# types.
//
//     Changes in this file made by drag-connecting in Xcode can be
//     synchronized back to C#, but more complex manual changes WILL NOT
//     transfer correctly.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------


#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>


@interface ViewController : NSViewController {{
	NSTextField * _FileLabel;
}}

@property (nonatomic, retain) IBOutlet NSTextField *FileLabel;

- (IBAction)UploadButton:(id)sender;

@end
");
		await File.WriteAllTextAsync (Path.Combine (tmpDir, "ViewController.m"),
$@"// ------------------------------------------------------------------------------
// <auto-generated>
//     Code was generated by Microsoft (R) xcsync tool.
//     This file was generated automatically to mirror C# types.
//
//     Changes in this file made by drag-connecting in Xcode can be
//     synchronized back to C#, but more complex manual changes WILL NOT
//     transfer correctly.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------


#import ""ViewController.h""

@implementation ViewController

@synthesize FileLabel = _FileLabel;

-(IBAction) UploadButton:(id) sender {{
}}

@end
");

	}

	static async Task AddControlAndOutletChangesFromDiff (ITestOutputHelper testOutput, string diff, string tmpDir, string projectType, string tfm)
	{
		var diffContents = await File.ReadAllTextAsync (diff);
		var projectName = Path.GetFileName (Path.GetDirectoryName (Path.GetDirectoryName (tmpDir)));
		var pbxproj = await File.ReadAllTextAsync (Path.Combine (tmpDir, $"{projectName}.xcodeproj/project.pbxproj"));

		diffContents = diffContents.Replace ("{{PROJECT}}", projectName);
		diffContents = diffContents.Replace ("{{PBXPROJ}}", pbxproj);

		var newDiff = Path.Combine (tmpDir, $"{projectName}.diff");
		await File.WriteAllTextAsync (newDiff, diffContents);

		await Patch (testOutput, tmpDir, newDiff).ConfigureAwait (false);
	}
}
