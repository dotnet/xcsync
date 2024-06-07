// Copyright (c) Microsoft Corporation.  All rights reserved.

using ClangSharp;
using ClangSharp.Interop;
using Moq;
using Serilog;
using xcsync.Ast;
using static ClangSharp.Interop.CXTranslationUnit_Flags;

namespace xcsync.tests.Ast;

public class ObjCSyntaxRewriterTest {
	const string DefaultInputFileName = "ClangUnsavedFile.h";

	[Theory]
	[InlineData (ViewControllerObjC, ViewControllerCSharp)]
	public async void WriteAsync_ObjCInterfaceDecl_VisitCalledForEachChild (string inputContents, string expectedOutput)
	{
		// Arrange

		// This test uses ClangSharp directly to parse the inputs into a Cursor
		// This is only to test the ObjCSyntaxRewriter, and would not be used in production code
		// So the hardcoding of the OS Arch and target platform is acceptable

		var logger = new Mock<ILogger> ();
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
		var walker = new ObjCSyntaxRewriter (logger.Object);
		var newSyntax = await walker!.WriteAsync (decl, null);

		// Assert
		Assert.NotNull (newSyntax);
		Assert.Equal (expectedOutput, newSyntax!.GetRoot().ToString ());
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
}
