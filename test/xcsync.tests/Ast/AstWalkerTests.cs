// Copyright (c) Microsoft Corporation.  All rights reserved.

using ClangSharp;
using ClangSharp.Interop;
using Moq;
using static ClangSharp.Interop.CXTranslationUnit_Flags;

namespace xcsync.tests.Ast;

public class AstWalkerTest {
	const string DefaultInputFileName = "ClangUnsavedFile.h";

	[Theory]
	[InlineData (6, BasicObjCInput)]
	[InlineData (17, SimpleViewControllerInput)]
	[InlineData (25, ConcreteViewControllerInput)]
	public void Walk_MultipleChildren_VisitCalledForEachChild (int expectedVisits, string inputContents)
	{
		// Arrange

		// This test uses ClangSharp directly to parse the inputs into a Cursor
		// This is only to test the AstWalker, and would not be used in production code
		// So the hardcoding of the OS Arch and target platform is acceptable

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

		var mockVisitor = new Mock<AstVisitor> ();

		// Act
		var walker = new AstWalker () as IWalker<Cursor, AstVisitor>;
		walker!.Walk (node.TranslationUnitDecl, mockVisitor.Object, (cursor) => {
			return !cursor.Location.IsInSystemHeader;
		});

		// Assert
		mockVisitor.Verify (v => v.VisitAsync (It.IsAny<Cursor> ()), Times.Exactly (expectedVisits));
	}

	[Theory]
	[InlineData (6, BasicObjCInput)]
	[InlineData (17, SimpleViewControllerInput)]
	[InlineData (25, ConcreteViewControllerInput)]
	public async void WalkAsync_MultipleChildren_VisitCalledForEachChild (int expectedVisits, string inputContents)
	{
		// Arrange

		// This test uses ClangSharp directly to parse the inputs into a Cursor
		// This is only to test the AstWalker, and would not be used in production code
		// So the hardcoding of the OS Arch and target platform is acceptable

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

		var mockVisitor = new Mock<AstVisitor> ();

		// Act

		var walker = new AstWalker ();
		await walker.WalkAsync (node.TranslationUnitDecl, mockVisitor.Object, (cursor) => {
			return !cursor.Location.IsInSystemHeader;
		});

		// Assert

		mockVisitor.Verify (v => v.VisitAsync (It.IsAny<Cursor> ()), Times.Exactly (expectedVisits));
	}

	const string BasicObjCInput = @"
struct MyStruct
{
    int _value;

    MyStruct(int value)
    {
        _value = value;
    }
};
";
	const string SimpleViewControllerInput = @"
#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>

@interface ViewController : NSViewController {
	NSTextField *_FileLabel;
}
	@property (nonatomic, retain) IBOutlet NSTextField *FileLabel;
 
	- (IBAction)UploadButton:(id)sender;
@end
";

	const string ConcreteViewControllerInput = @"
#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>

@interface ViewController : NSViewController {
	NSTextField *_FileLabel;
}
	@property (nonatomic, retain) IBOutlet NSTextField *FileLabel;
 
	- (IBAction)UploadButton:(id)sender;
@end

@implementation ViewController
 
	@synthesize FileLabel = _FileLabel;
	
	- (IBAction)UploadButton:(id)sender {
	}
 
@end
";
}
