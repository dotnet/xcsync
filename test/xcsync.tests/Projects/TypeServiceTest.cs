// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
	public void AddCompilation_FindsTypesInNestedNamespaces ()
	{
		var compilation = CreateCompilation (
			"""
			using System;

			namespace Foundation {
				[AttributeUsage (AttributeTargets.Class)]
				public sealed class RegisterAttribute : Attribute {
				}

				[Register]
				public class NSObject {
				}
			}

			namespace AppKit {
				[Foundation.Register]
				public class NSViewController : Foundation.NSObject {
				}
			}

			namespace TestProject.Controllers.Nested {
				[Foundation.Register]
				public class NestedViewController : AppKit.NSViewController {
				}
			}
			""");

		typeService.AddCompilation ("macos", compilation);

		var result = Assert.Single (typeService.QueryTypes (clrType: "NestedViewController"));
		Assert.NotNull (result);
		Assert.True (result.IsInSource);
		Assert.Equal ("NestedViewController", result.ObjCType);
		Assert.Equal ("NSViewController", result.BaseType?.ClrType);
	}

	[Fact]
	public async Task TryUpdateMappingAsync_FindsTypesInNestedNamespaces ()
	{
		var source =
			"""
			using System;

			namespace Foundation {
				[AttributeUsage (AttributeTargets.Class)]
				public sealed class RegisterAttribute : Attribute {
				}

				[Register]
				public class NSObject {
				}
			}

			namespace AppKit {
				[Foundation.Register]
				public class NSViewController : Foundation.NSObject {
				}
			}

			namespace TestProject.Controllers.Nested {
				[Foundation.Register]
				public class NestedViewController : AppKit.NSViewController {
				}
			}
			""";
		var updatedSource =
			"""
			using System;

			namespace Foundation {
				[AttributeUsage (AttributeTargets.Class)]
				public sealed class RegisterAttribute : Attribute {
				}

				[Register]
				public class NSObject {
				}
			}

			namespace AppKit {
				[Foundation.Register]
				public class NSViewController : Foundation.NSObject {
				}
			}

			namespace TestProject.Controllers.Nested {
				[Foundation.Register]
				public class NestedViewController : AppKit.NSViewController {
					public int Value => 1;
				}
			}
			""";
		var compilation = CreateCompilation (source);
		typeService.AddCompilation ("macos", compilation);

		var existingMapping = Assert.Single (typeService.QueryTypes (clrType: "NestedViewController"));
		Assert.NotNull (existingMapping);

		var updatedTree = CSharpSyntaxTree.ParseText (updatedSource, path: "NestedViewController.cs");
		var updatedRoot = await updatedTree.GetRootAsync ();
		var updatedType = updatedRoot.DescendantNodes ().OfType<ClassDeclarationSyntax> ()
			.Single (node => node.Identifier.ValueText == "NestedViewController");

		var result = await typeService.TryUpdateMappingAsync (existingMapping, updatedType);

		Assert.True (result);
		Assert.True (Assert.Single (typeService.QueryTypes (clrType: "NestedViewController"))?.HasChanges);
	}

	static Compilation CreateCompilation (string source)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText (source, path: "NestedViewController.cs");

		return CSharpCompilation.Create (
			"NestedNamespaceTests",
			[syntaxTree],
			[
				MetadataReference.CreateFromFile (typeof (object).Assembly.Location),
			],
			new CSharpCompilationOptions (OutputKind.DynamicallyLinkedLibrary));
	}
}
