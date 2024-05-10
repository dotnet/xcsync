// Copyright (c) Microsoft Corporation.  All rights reserved.

using xcsync.Projects;

namespace xcsync.tests.Projects;

public class GenObjcFileTest : Base {

	const string warning =
		"""
		// ------------------------------------------------------------------------------
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



		""";

	[Fact]
	public async Task GenerateAppDelegateHFile ()
	{
		(_, NSProject xcodeProject) = InitializeProjects ();

		var types = await xcodeProject.GetTypes ().ToListAsync ().ConfigureAwait (false);
		var nsType = types.Select (x => x).FirstOrDefault (x => x.CliType == "AppDelegate");

		Assert.NotNull (nsType);

		var generated = new GenObjcH (nsType).TransformText ();

		const string expected =
			warning +
			"""
			#import <AppKit/AppKit.h>
			#import <Foundation/Foundation.h>
			
			
			@interface AppDelegate : NSObject {
			}
			
			@end
			
			""";

		Assert.Equal (expected, generated);
	}

	[Fact]
	public async Task GenerateAppDelegateMFile ()
	{
		(_, NSProject xcodeProject) = InitializeProjects ();

		var types = await xcodeProject.GetTypes ().ToListAsync ().ConfigureAwait (false);
		var nsType = types.Select (x => x).FirstOrDefault (x => x.CliType == "AppDelegate");

		Assert.NotNull (nsType);

		var generated = (new GenObjcM (nsType) as ITextTemplate).TransformText ();

		const string expected =
			warning +
			"""
			#import "AppDelegate.h"
			
			@implementation AppDelegate
			
			@end

			""";

		Assert.Equal (expected, generated);
	}

	[Fact]
	public async Task GenerateViewControllerHFile ()
	{
		(_, NSProject xcodeProject) = InitializeProjects ();

		var types = await xcodeProject.GetTypes ().ToListAsync ().ConfigureAwait (false);
		var nsType = types.Select (x => x).FirstOrDefault (x => x.CliType == "ViewController");

		Assert.NotNull (nsType);

		var generated = (new GenObjcH (nsType) as ITextTemplate).TransformText ();

		const string expected =
			warning +
			"""
			#import <AppKit/AppKit.h>
			#import <Foundation/Foundation.h>


			@interface ViewController : NSViewController {
				NSTextField *_FileLabel;
			}

			@property (nonatomic, retain) IBOutlet NSTextField *FileLabel;

			- (IBAction)UploadButton:(id)sender;

			@end

			""";

		Assert.Equal (expected, generated);
	}

	[Fact]
	public async Task GenerateViewControllerMFile ()
	{
		(_, NSProject xcodeProject) = InitializeProjects ();

		var types = await xcodeProject.GetTypes ().ToListAsync ().ConfigureAwait (false);
		var nsType = types.Select (x => x).FirstOrDefault (x => x.CliType == "ViewController");

		Assert.NotNull (nsType);

		var generated = (new GenObjcM (nsType) as ITextTemplate).TransformText ();

		const string expected =
			warning +
			"""
			#import "ViewController.h"
			
			@implementation ViewController
			
			@synthesize FileLabel = _FileLabel;
			
			- (IBAction)UploadButton:(id)sender {
			}
			
			@end

			""";

		Assert.Equal (expected, generated);
	}

	(Dotnet, NSProject) InitializeProjects ()
	{
		Assert.True (File.Exists (TestProjectPath));

		var cliProject = new Dotnet (TestProjectPath, "net8.0-macos");
		var xcodeProject = new NSProject (cliProject, "macos");
		return (cliProject, xcodeProject);
	}

}
