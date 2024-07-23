// Copyright (c) Microsoft Corporation. All rights reserved.

using Xamarin;
using Xunit.Abstractions;

namespace xcsync.e2e.tests.UseCases;

public partial class GenerateThenSyncWithChangesTests (ITestOutputHelper testOutput) : Base (testOutput) {
	public static IEnumerable<object []> AddControlAndOutlet =>
	[
			["macos", "net8.0-macos", (string path, string projectType, string tfm) => AddControlAndOutletChanges (path, projectType, tfm)],
		// ["maccatalyst", "net8.0-maccatalyst", (string path, string projectType, string tfm) => AddControlAndOutletChanges (path, projectType, tfm)], 
		["ios", "net8.0-ios", (string path, string projectType, string tfm) => AddControlAndOutletChanges (path, projectType, tfm)],
		// ["tvos", "net8.0-tvos", (string path, string projectType, string tfm) => AddControlAndOutletChanges (path, projectType, tfm)],
		["maui", "net8.0-ios", (string path, string projectType, string tfm) => AddControlAndOutletChanges (path, projectType, tfm)],
		// ["maui", "net8.0-maccatalyst", (string path, string projectType, string tfm) => AddControlAndOutletChanges (path, projectType, tfm)]
	];

	[Theory]
	[MemberData (nameof (AddControlAndOutlet))]
	[Trait ("Category", "IntegrationTest")]
	public async Task GenerateThenSync_WithChanges_GeneratesChangesAsync (string projectType, string tfm, Func<string, string, string, Task> makeChanges)
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
		await makeChanges (tmpDir, projectType, tfm).ConfigureAwait (false);
		await Xcsync (TestOutput, "sync", "--project", csproj, "--target", xcodeDir, "-tfm", tfm).ConfigureAwait (false);

		// Assert

		var changesPresent = await Git (TestOutput, "-C", tmpDir, "diff-index", "--quiet", "HEAD", "--exit-code", "--").ConfigureAwait (false);
		if (changesPresent == 0)
			Assert.Fail ("Git diff-index failed, there are no changes in the source files.");
	}

	static async Task AddControlAndOutletChanges (string tmpDir, string projectType, string tfm)
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
}
