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
	[InlineData (ViewControllerCustomOutletObjC, ViewControllerCustomOutletCSharp)]
	[InlineData (ViewControllerActionObjC, ViewControllerActionCSharp)]
	public async void WriteAsync_ObjCInterfaceDecl_TranslatesToCorrectValidSyntaxTree (string inputContents, string expectedOutput)
	{
		// Arrange

		// This test uses ClangSharp directly to parse the inputs into a Cursor
		// This is only to test the ObjCSyntaxRewriter, and would not be used in production code
		// So the hardcoding of the OS Arch and target platform is acceptable

		var logger = new Mock<ILogger> ();
		var TypeService = new Mock<TypeService> (logger.Object);
		var NSTextFieldTypeMapping = new TypeMapping (CreateTypeSymbol ("NSTextField", "AppKit"), "NSTextField", "NSTextField", null, false, false, false, null, null, []);
		var PrimaryButtonTypeMapping = new TypeMapping (CreateTypeSymbol ("PrimaryButton", "TestCustomControl"), "PrimaryButton", "PrimaryButton", null, false, false, false, null, null, []);

		TypeService.Setup (
			x => x.QueryTypes (It.IsAny<string> (), It.Is<string> (s => s == "NSTextField"))
		).Returns ([NSTextFieldTypeMapping]);
		TypeService.Setup (
			x => x.QueryTypes (It.IsAny<string> (), It.Is<string> (s => s == "PrimaryButton"))
		).Returns ([PrimaryButtonTypeMapping]);
		TypeService.Setup (
			x => x.QueryTypes (It.IsAny<string> (), It.Is<string> (s => s == "NSButton"))
		).Returns ([CreateTypeMapping ("NSButton", "AppKit")]);

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

	[Theory]
	[InlineData (false, ViewControllerExplicitActionCSharpImplicit)]
	[InlineData (true, ViewControllerExplicitActionCSharpExplicit)]
	public async void WriteAsync_ObjCInterfaceDecl_WithExplicitTypes_UsesActionParameterTypeWhenEnabled (bool explicitTypes, string expectedOutput)
	{
		var logger = new Mock<ILogger> ();
		var TypeService = new Mock<TypeService> (logger.Object);

		TypeService.Setup (
			x => x.QueryTypes (It.IsAny<string> (), It.Is<string> (s => s == "PrimaryButton"))
		).Returns ([CreateTypeMapping ("PrimaryButton", "TestCustomControl")]);

		var directory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
		Directory.CreateDirectory (directory);
		var inputFileName = Path.Combine (directory, "ClangUnsavedFile.h");
		var primaryButtonFileName = Path.Combine (directory, "PrimaryButton.h");
		await File.WriteAllTextAsync (inputFileName, ViewControllerExplicitActionObjC);
		await File.WriteAllTextAsync (primaryButtonFileName, PrimaryButtonObjC);
		var index = CXIndex.Create ();

		CXTranslationUnit_Flags DefaultTranslationUnitFlags = CXTranslationUnit_None
			| CXTranslationUnit_IncludeAttributedTypes
			| CXTranslationUnit_VisitImplicitAttributes;

		string [] clangCommandLineArgs = [
				"-x",
			"objective-c",
			"-target",
			"arm64-apple-macosx",
			"-isysroot",
			Path.Combine (Scripts.SelectXcode (), "Contents", "Developer", "Platforms", "MacOSX.platform", "Developer", "SDKs", "MacOSX.sdk"),
		];

		var translationUnitError = CXTranslationUnit.TryParse (index, inputFileName, clangCommandLineArgs, [], DefaultTranslationUnitFlags, out var handle);
		using var node = TranslationUnit.GetOrCreate (handle);

		var cursor = node.TranslationUnitDecl.CursorChildren [^2];
		var decl = (ObjCInterfaceDecl) cursor;

		var walker = new ObjCSyntaxRewriter (logger.Object, TypeService.Object, new AdhocWorkspace (), explicitTypes);
		var newSyntax = await walker.WriteAsync (decl, null);
		var actualOutput = newSyntax!.GetRoot ().ToFullString ();

		Assert.NotNull (newSyntax);
		Assert.Equal (expectedOutput, actualOutput);
		Directory.Delete (directory, true);
	}

	[Fact]
	public async void WriteAsync_ObjCInterfaceDecl_WithMissingImport_UsesSourceTypeFallback ()
	{
		var logger = new Mock<ILogger> ();
		var TypeService = new Mock<TypeService> (logger.Object);

		TypeService.Setup (
			x => x.QueryTypes (It.IsAny<string> (), It.Is<string> (s => s == "WKWebView"))
		).Returns ([CreateTypeMapping ("WKWebView", "WebKit")]);

		var directory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
		Directory.CreateDirectory (directory);
		var inputFileName = Path.Combine (directory, "ClangUnsavedFile.h");
		await File.WriteAllTextAsync (inputFileName, ViewControllerMissingImportOutletObjC);
		var index = CXIndex.Create ();

		CXTranslationUnit_Flags DefaultTranslationUnitFlags = CXTranslationUnit_None
			| CXTranslationUnit_IncludeAttributedTypes
			| CXTranslationUnit_VisitImplicitAttributes;

		string [] clangCommandLineArgs = [
				"-x",
			"objective-c",
			"-target",
			"arm64-apple-macosx",
			"-isysroot",
			Path.Combine (Scripts.SelectXcode (), "Contents", "Developer", "Platforms", "MacOSX.platform", "Developer", "SDKs", "MacOSX.sdk"),
		];

		var translationUnitError = CXTranslationUnit.TryParse (index, inputFileName, clangCommandLineArgs, [], DefaultTranslationUnitFlags, out var handle);
		using var node = TranslationUnit.GetOrCreate (handle);

		var cursor = node.TranslationUnitDecl.CursorChildren [^2];
		var decl = (ObjCInterfaceDecl) cursor;

		var walker = new ObjCSyntaxRewriter (logger.Object, TypeService.Object, new AdhocWorkspace ());
		var newSyntax = await walker.WriteAsync (decl, null);
		var actualOutput = newSyntax!.GetRoot ().ToFullString ();

		Assert.NotNull (newSyntax);
		Assert.Equal (ViewControllerMissingImportOutletCSharp, actualOutput);
		Directory.Delete (directory, true);
	}

	[Fact]
	public async void WriteAsync_ObjCInterfaceDecl_WithUnresolvedOutlet_LogsErrorAndSkipsProperty ()
	{
		var logger = new Mock<ILogger> ();
		var TypeService = new Mock<TypeService> (logger.Object);

		var directory = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
		Directory.CreateDirectory (directory);
		var inputFileName = Path.Combine (directory, "ClangUnsavedFile.h");
		await File.WriteAllTextAsync (inputFileName, ViewControllerUnresolvedOutletObjC);
		var index = CXIndex.Create ();

		CXTranslationUnit_Flags DefaultTranslationUnitFlags = CXTranslationUnit_None
			| CXTranslationUnit_IncludeAttributedTypes
			| CXTranslationUnit_VisitImplicitAttributes;

		string [] clangCommandLineArgs = [
				"-x",
			"objective-c",
			"-target",
			"arm64-apple-macosx",
			"-isysroot",
			Path.Combine (Scripts.SelectXcode (), "Contents", "Developer", "Platforms", "MacOSX.platform", "Developer", "SDKs", "MacOSX.sdk"),
		];

		var translationUnitError = CXTranslationUnit.TryParse (index, inputFileName, clangCommandLineArgs, [], DefaultTranslationUnitFlags, out var handle);
		using var node = TranslationUnit.GetOrCreate (handle);

		var cursor = node.TranslationUnitDecl.CursorChildren [^2];
		var decl = (ObjCInterfaceDecl) cursor;

		var walker = new ObjCSyntaxRewriter (logger.Object, TypeService.Object, new AdhocWorkspace ());
		var newSyntax = await walker.WriteAsync (decl, null);
		var actualOutput = newSyntax!.GetRoot ().ToFullString ();

		Assert.NotNull (newSyntax);
		Assert.Equal (ViewControllerCSharp, actualOutput);
		logger.Verify (x => x.Error (It.Is<string> (s => s.Contains ("MysteryView") && s.Contains ("WebView"))), Times.Once);
		Directory.Delete (directory, true);
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
    AppKit.NSTextField Name { get; set; }

    void ReleaseDesignerOutlets()
    {
        if (Name != null)
        {
            Name.Dispose();
            Name = null;
        }
    }
}";
	const string ViewControllerCustomOutletObjC = @"
#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>

@class PrimaryButton;

@interface ViewController : NSViewController {
}

@property (weak) IBOutlet PrimaryButton *CustomButton;

@end

@implementation ViewController
 
@end
";
	const string ViewControllerCustomOutletCSharp = @"[Register(""ViewController"")]
partial class ViewController
{
    [Outlet]
    TestCustomControl.PrimaryButton CustomButton { get; set; }

    void ReleaseDesignerOutlets()
    {
        if (CustomButton != null)
        {
            CustomButton.Dispose();
            CustomButton = null;
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
	const string ViewControllerExplicitActionObjC = @"
#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>
#import ""PrimaryButton.h""

@interface ViewController : NSViewController {
}

- (IBAction)Button:(id)sender;
- (IBAction)OnButtonClick2:(PrimaryButton *)sender;

@end

@implementation ViewController
 
@end
";
const string ViewControllerExplicitActionCSharpImplicit = @"[Register(""ViewController"")]
partial class ViewController
{
    [Action(""Button:"")]
    partial void Button(Foundation.NSObject sender);
    [Action(""OnButtonClick2:"")]
    partial void OnButtonClick2(Foundation.NSObject sender);

    void ReleaseDesignerOutlets()
    {
    }
}";
const string ViewControllerExplicitActionCSharpExplicit = @"[Register(""ViewController"")]
partial class ViewController
{
    [Action(""Button:"")]
    partial void Button(Foundation.NSObject sender);
    [Action(""OnButtonClick2:"")]
    partial void OnButtonClick2(TestCustomControl.PrimaryButton sender);

    void ReleaseDesignerOutlets()
    {
    }
}";
	const string ViewControllerMissingImportOutletObjC = @"
#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>

@interface ViewController : NSViewController {
}

@property (strong) IBOutlet WKWebView *WebView;

@end

@implementation ViewController
 
@end
";
	const string ViewControllerMissingImportOutletCSharp = @"[Register(""ViewController"")]
partial class ViewController
{
    [Outlet]
    WebKit.WKWebView WebView { get; set; }

    void ReleaseDesignerOutlets()
    {
        if (WebView != null)
        {
            WebView.Dispose();
            WebView = null;
        }
    }
}";
	const string ViewControllerUnresolvedOutletObjC = @"
#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>

@interface ViewController : NSViewController {
}

@property (strong) IBOutlet MysteryView *WebView;

@end

@implementation ViewController
 
@end
";
	const string PrimaryButtonObjC = @"
#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>

@interface PrimaryButton : NSButton
@end
";

	static INamedTypeSymbol CreateTypeSymbol (string name, string? namespaceName)
	{
		var typeSymbol = new Mock<INamedTypeSymbol> ();
		typeSymbol.Setup (x => x.Name).Returns (name);
		typeSymbol.Setup (x => x.MetadataName).Returns (name);

		var namespaceSymbol = new Mock<INamespaceSymbol> ();
		namespaceSymbol.Setup (x => x.IsGlobalNamespace).Returns (string.IsNullOrEmpty (namespaceName));
		namespaceSymbol.Setup (x => x.ToDisplayString (null)).Returns (namespaceName ?? string.Empty);
		typeSymbol.Setup (x => x.ContainingNamespace).Returns (namespaceSymbol.Object);

		return typeSymbol.Object;
	}

	static TypeMapping CreateTypeMapping (string name, string? namespaceName) =>
		new (CreateTypeSymbol (name, namespaceName), name, name, null, false, false, false, null, null, []);
}
