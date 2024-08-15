// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ClangSharp;
using ClangSharp.Interop;
using Microsoft.CodeAnalysis;
using Moq;
using Serilog;
using xcsync.Ast;
using xcsync.Projects;
using static ClangSharp.Interop.CXTranslationUnit_Flags;

namespace xcsync.tests.Ast;

public class ObjCSyntaxRewriterTest {
	const string DefaultInputFileName = "ClangUnsavedFile.h";

	[Theory]
	[InlineData (ViewControllerObjC, ViewControllerCSharp)]
	[InlineData (ViewControllerOutletObjC, ViewControllerOutletCSharp)]
	[InlineData (ViewControllerActionObjC, ViewControllerActionCSharp)]
	public async void WriteAsync_ObjCInterfaceDecl_TranslatesToCorrectValidSyntaxTree (string inputContents, string expectedOutput)
	{
		// Arrange

		// This test uses ClangSharp directly to parse the inputs into a Cursor
		// This is only to test the ObjCSyntaxRewriter, and would not be used in production code
		// So the hardcoding of the OS Arch and target platform is acceptable

		var logger = new Mock<ILogger> ();
		var TypeService = new Mock<TypeService> (logger.Object);
		var NSTextFieldTypeMapping = new TypeMapping (null, "NSTextField", "NSTextField", null, false, false, false, null, null, []);

		TypeService.Setup (
			x => x.QueryTypes (It.IsAny<string> (), It.Is<string> (s => s == "NSTextField"))
		).Returns ([NSTextFieldTypeMapping]);

		var index = CXIndex.Create ();
		using var unsavedFile = CXUnsavedFile.Create (DefaultInputFileName, inputContents);

		CXTranslationUnit_Flags DefaultTranslationUnitFlags = CXTranslationUnit_None
			| CXTranslationUnit_IncludeAttributedTypes      // Include attributed types in CXType
			| CXTranslationUnit_VisitImplicitAttributes;    // Implicit attributes should be visited;

		string [] clangCommandLineArgs = [
				"-x",
			"objective-c",
			"-target",
			"arm64-apple-macosx",
			"-isysroot",
			Path.Combine (Scripts.SelectXcode (), "Contents", "Developer", "Platforms", "MacOSX.platform", "Developer", "SDKs", "MacOSX.sdk"),
		];

		var translationUnitError = CXTranslationUnit.TryParse (index, DefaultInputFileName, clangCommandLineArgs, [unsavedFile], DefaultTranslationUnitFlags, out var handle);
		using var node = TranslationUnit.GetOrCreate (handle);

		var cursor = node.TranslationUnitDecl.CursorChildren [^2];
		var decl = (ObjCInterfaceDecl) cursor;

		// Act
		var walker = new ObjCSyntaxRewriter (logger.Object, TypeService.Object, new AdhocWorkspace ());
		var newSyntax = await walker!.WriteAsync (decl, null);
		var actualOutput = newSyntax!.GetRoot ().ToFullString ();

		// Assert
		Assert.NotNull (newSyntax);
		Assert.Equal (expectedOutput, actualOutput);
	}

	const string ViewControllerObjC = @"
#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>

@interface ViewController : NSViewController {
}
@end

@implementation ViewController
 
@end
";
	const string ViewControllerCSharp = @"[Register(""ViewController"")]
partial class ViewController
{
    void ReleaseDesignerOutlets()
    {
    }
}";
	const string ViewControllerOutletObjC = @"
#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>

@interface ViewController : NSViewController {
}

@property (weak) IBOutlet NSTextField *Name;

@end

@implementation ViewController
 
@end
";
	const string ViewControllerOutletCSharp = @"[Register(""ViewController"")]
partial class ViewController
{
    [Outlet]
    NSTextField Name { get; set; }

    void ReleaseDesignerOutlets()
    {
        if (Name != null)
        {
            Name.Dispose();
            Name = null;
        }
    }
}";
	const string ViewControllerActionObjC = @"
#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>

@interface ViewController : NSViewController {
}

- (IBAction)HelloWorld:(id)sender;

@end

@implementation ViewController
 
 - (IBAction)HelloWorld:(id)sender {
}
@end
";
	const string ViewControllerActionCSharp = @"[Register(""ViewController"")]
partial class ViewController
{
    [Action(""HelloWorld:"")]
    partial void HelloWorld(Foundation.NSObject sender);

    void ReleaseDesignerOutlets()
    {
    }
}";
}
