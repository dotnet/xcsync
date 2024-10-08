// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Moq;
using Serilog;
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
	public void GenerateAppDelegateHFile ()
	{
		(_, ITypeService typeService) = InitializeProjects ();

		var types = typeService.QueryTypes ().ToList ();
		var nsType = types.Select (x => x).FirstOrDefault (x => x?.ClrType == "AppDelegate");

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
	public void GenerateAppDelegateMFile ()
	{
		(_, ITypeService typeService) = InitializeProjects ();

		var types = typeService.QueryTypes ().ToList ();
		var nsType = types.Select (x => x).FirstOrDefault (x => x?.ClrType == "AppDelegate");

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
	public void GenerateViewControllerHFile ()
	{
		(_, ITypeService typeService) = InitializeProjects ();

		var types = typeService.QueryTypes ().ToList ();
		var nsType = types.Select (x => x).FirstOrDefault (x => x?.ClrType == "ViewController");

		Assert.NotNull (nsType);

		var generated = new GenObjcH (nsType).TransformText ();

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
	public void GenerateViewControllerMFile ()
	{
		(_, ITypeService typeService) = InitializeProjects ();

		var types = typeService.QueryTypes ().ToList ();
		var nsType = types.Select (x => x).FirstOrDefault (x => x?.ClrType == "ViewController");

		Assert.NotNull (nsType);

		var generated = new GenObjcM (nsType).TransformText ();

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

	(ClrProject, ITypeService) InitializeProjects ()
	{
		Assert.True (File.Exists (TestProjectPath));
		// TODO: Convert this to MockFileSystem
		var typeService = new TypeService (Mock.Of<ILogger> ());
		var clrProject = new ClrProject (new FileSystem (), Mock.Of<ILogger> (), typeService, "TestProject", TestProjectPath, "net8.0-macos");
		clrProject.OpenProject ().Wait ();
		return (clrProject, typeService);
	}

}
