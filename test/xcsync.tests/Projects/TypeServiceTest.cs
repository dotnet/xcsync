// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Moq;
using Serilog;
using xcsync.Projects;

namespace xcsync.tests.Projects;

public class TypeServiceTest {
	readonly TypeService typeService;

	public TypeServiceTest ()
	{
		typeService = new TypeService (Mock.Of<ILogger> ());

		typeService.AddType (Mock.Of<INamedTypeSymbol> (), "ViewController", "ViewController", null, false, false, false, null, null, []);
		typeService.AddType (Mock.Of<INamedTypeSymbol> (), "AppDelegate", "AppDelegate", null, false, false, false, null, null, []);
		typeService.AddType (Mock.Of<INamedTypeSymbol> (), "TypeExtension", "TypeExtension", null, false, false, false, null, null, []);
	}

	[Fact]
	public void AddType ()
	{
		var type = new TypeMapping (Mock.Of<INamedTypeSymbol> (), "NewType", "NewType", null, false, false, false, null, null, []);
		var result = typeService.AddType (type);
		Assert.NotNull (result);
		Assert.Equal (type, result);
	}

	[Fact]
	public void QueryExistentTypes ()
	{
		var type = new TypeMapping (Mock.Of<INamedTypeSymbol> (), "NewTypeClr", "NewTypeObjC", null, false, false, false, null, null, []);
		typeService.AddType (type);
		var result = typeService.QueryTypes (objcType: "NewTypeObjC");
		Assert.Single (result);
		Assert.Equal (type, result.First ());
	}

	[Fact]
	public void QueryNonexistentType ()
	{
		var result = typeService.QueryTypes (clrType: "doesNotExist");
		Assert.Empty (result);
	}

	[Fact]
	public void QueryMultipleTypes ()
	{
		var result = typeService.QueryTypes ();
		Assert.Equal (3, result.Count ());
	}

	[Fact]
	public void AddDuplicateClrType_ReturnsNull ()
	{
		var viewControllerDupe = new TypeMapping (Mock.Of<INamedTypeSymbol> (), "ViewController", "ViewController", null, false, false, false, null, null, []);
		typeService.AddType (viewControllerDupe);
		var result = typeService.AddType (viewControllerDupe);
		Assert.Null (result);
	}

	[Fact]
	public void AddDuplicateObjCType_ReturnsNull ()
	{
		var appDelegateDupe = new TypeMapping (Mock.Of<INamedTypeSymbol> (), "NewAppDelegate", "AppDelegate", null, false, false, false, null, null, []);
		typeService.AddType (appDelegateDupe);
		var result = typeService.AddType (appDelegateDupe);
		Assert.Null (result);
	}

	[Fact]
	public void AddCompilation_WithCustomControlAction_AddsHeaderReference ()
	{
		var localTypeService = new TypeService (Mock.Of<ILogger> ());
		var nsObject = new TypeMapping (Mock.Of<INamedTypeSymbol> (), "NSObject", "NSObject", null, false, false, false, null, null, ["Foundation"]);
		localTypeService.AddType (nsObject);
		localTypeService.AddType (Mock.Of<INamedTypeSymbol> (), "NSViewController", "NSViewController", nsObject, false, false, false, null, null, ["AppKit", "Foundation"]);
		localTypeService.AddType (Mock.Of<INamedTypeSymbol> (), "NSButton", "NSButton", nsObject, false, false, false, null, null, ["AppKit", "Foundation"]);

		var compilation = CSharpCompilation.Create ("TestAssembly",
			[CSharpSyntaxTree.ParseText (
				"""
				using Foundation;
				using AppKit;

				namespace Foundation {
				public class RegisterAttribute : System.Attribute
				{
					public RegisterAttribute (string name) { }
				}

				public class ActionAttribute : System.Attribute
				{
					public ActionAttribute (string name) { }
				}
				}

				namespace AppKit {
				public class NSObject { }
				public class NSViewController : NSObject { }
				public class NSButton : NSObject { }
				}

				namespace TestCustomControl {
				[Register ("PrimaryButton")]
				public class PrimaryButton : NSButton { }
				}

				namespace TestApp {
				[Register ("ViewController")]
				public partial class ViewController : NSViewController
				{
					[Action ("OnCancelButtonClicked:")]
					partial void OnCancelButtonClicked (TestCustomControl.PrimaryButton sender);
				}
				}
				""")],
			[MetadataReference.CreateFromFile (typeof (object).Assembly.Location)]);

		localTypeService.AddCompilation ("macos", compilation);

		var result = localTypeService.QueryTypes (clrType: "ViewController").Single ();

		Assert.NotNull (result);
		Assert.Contains ("PrimaryButton", result!.HeaderReferences);
		Assert.NotNull (result.Actions);
		Assert.Single (result.Actions!);
		Assert.Single (result.Actions [0].Parameters);
		Assert.Equal ("PrimaryButton", result.Actions [0].Parameters [0].ClrType);
		Assert.Equal ("PrimaryButton", result.Actions [0].Parameters [0].ObjCType);
	}
}
