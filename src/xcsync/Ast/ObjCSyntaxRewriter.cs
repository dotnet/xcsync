// Copyright (c) Microsoft Corporation.  All rights reserved.

using ClangSharp;
using ClangSharp.Interop;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Serilog;
using xcsync.Projects;
using static ClangSharp.Interop.CX_DeclKind;
using static ClangSharp.Interop.CXTypeKind;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace xcsync.Ast;

class ObjCSyntaxRewriter (ILogger Logger, ITypeService typeService, Workspace workspace) : AstWalker {

	internal async Task<SyntaxTree?> WriteAsync (ObjCInterfaceDecl objcType, SyntaxTree? syntaxTree)
	{
		var visitor = new Visitor (Logger, typeService, syntaxTree);
		await WalkAsync (objcType, visitor).ConfigureAwait (false);

		// Now that we have the basic tree, lets make sure it generates pretty C# code
		var sortedTree = SortClassMembers (visitor.SyntaxTree!);
		var root = sortedTree!.GetRoot ();
		root = Formatter.Format (root, Formatter.Annotation, workspace);
		root = Formatter.Format (root, SyntaxAnnotation.ElasticAnnotation, workspace);
		root = root.NormalizeWhitespace ("\t", Environment.NewLine, false);

		// Add a newline between each method, so that the generated code is easier to read
		// except for the first method, which should not have a newline before it
		root = root.ReplaceNodes (root.DescendantNodes ().OfType<MethodDeclarationSyntax> ().Skip (1), (original, rewritten) => {
			return rewritten.WithLeadingTrivia (original.GetLeadingTrivia ().Insert (0, Whitespace (Environment.NewLine)));
		});
		return root.SyntaxTree;
	}

	class Visitor (ILogger logger, ITypeService typeService, SyntaxTree? syntaxTree) : AstVisitor {

		public SyntaxTree? SyntaxTree { get; private set; } = syntaxTree;

		protected override Task VisitDeclAsync (Decl decl)
		{
			logger.Debug ($"[{nameof (ObjCSyntaxRewriter)}] Visiting {{Kind}}", decl.DeclKindName);
			switch (decl.Kind) {
			case CX_DeclKind_ObjCInterface:
				var objcImpl = decl as ObjCInterfaceDecl;
				Write (objcImpl!);
				break;
			case CX_DeclKind_ObjCProperty:
				var objcProperty = decl as ObjCPropertyDecl;
				Write (objcProperty!);
				break;
			case CX_DeclKind_ObjCMethod:
				var objcMethod = decl as ObjCMethodDecl;
				Write (objcMethod!);
				break;

			};
			return Task.CompletedTask;
		}

		void Write (ObjCInterfaceDecl objcImpl)
		{
			// Create the ReleaseDesignerOutlets method
			var releaseDesignerOutletsMethod = MethodDeclaration (PredefinedType (Token (SyntaxKind.VoidKeyword)), "ReleaseDesignerOutlets")
				// .AddModifiers (Token (SyntaxKind.PublicKeyword))
				.WithBody (Block ());

			// Create a new class declaration
			var classDeclaration = ClassDeclaration (objcImpl.Name)
				// .AddModifiers (Token (SyntaxKind.PublicKeyword))
				.AddModifiers (Token (SyntaxKind.PartialKeyword))
							.AddAttributeLists (
				AttributeList (SingletonSeparatedList (
					Attribute (IdentifierName ("Register"),
						AttributeArgumentList (SingletonSeparatedList (
							AttributeArgument (LiteralExpression (SyntaxKind.StringLiteralExpression, Literal (objcImpl.Name)))))))))
				.AddMembers (releaseDesignerOutletsMethod);

			// Create a compilation unit with the namespace
			var compilationUnit = CompilationUnit ()
				.AddMembers (classDeclaration);

			// Create a new syntax tree with the compilation unit
			SyntaxTree = SyntaxTree (compilationUnit);
		}

		void Write (ObjCPropertyDecl objcProperty)
		{
			if (objcProperty.Attrs.ToList ().FirstOrDefault (a => a.Kind == CX_AttrKind.CX_AttrKind_IBOutlet) is null)
				return;

			var root = (CompilationUnitSyntax) SyntaxTree!.GetRoot ();

			var firstClass = root.DescendantNodes ().OfType<ClassDeclarationSyntax> ().First ();

			var propertyName = objcProperty.Name;

			// TODO: This is a *very* primitive way to get the property type and will need improvement
			// TODO: Need a solution to handle the case where the property type is not found  or is null in the type mapping
			var propertyType = objcProperty.Type switch { { Kind: CXType_ObjCObjectPointer } => typeService.QueryTypes (null, objcProperty.Type.AsString.Split (' ') [0]).First ()?.ClrType ?? string.Empty,
				_ => throw new NotImplementedException ($"Unsupported property type {objcProperty.Type.KindSpelling}")
			};

			// Create the property
			var property = PropertyDeclaration (ParseTypeName (propertyType), propertyName)
				// .AddModifiers (Token (SyntaxKind.PublicKeyword))
				.AddAccessorListAccessors (
					AccessorDeclaration (SyntaxKind.GetAccessorDeclaration)
						.WithSemicolonToken (Token (SyntaxKind.SemicolonToken)),
					AccessorDeclaration (SyntaxKind.SetAccessorDeclaration)
						.WithSemicolonToken (Token (SyntaxKind.SemicolonToken)))
				.AddAttributeLists (
					AttributeList (
						SingletonSeparatedList (Attribute (IdentifierName ("Outlet")))));

			// Add the property to the class
			var members = firstClass.Members.ToList ();
			members.Add (property);

			// Create a new syntax tree with the updated class
			var releaseDesignerOutletsMethod = members.OfType<MethodDeclarationSyntax> ()
				.FirstOrDefault (m => m.Identifier.Text == "ReleaseDesignerOutlets");

			if (releaseDesignerOutletsMethod != null) {
				var disposeStatement = ParseStatement ($"if ({propertyName} != null) {{ {propertyName}.Dispose (); {propertyName} = null; }}");
				var newReleaseDesignerOutletsMethod = releaseDesignerOutletsMethod.AddBodyStatements (disposeStatement);
				members [members.IndexOf (releaseDesignerOutletsMethod)] = newReleaseDesignerOutletsMethod;
			}

			var newClass = firstClass.WithMembers (List (members));

			var newRoot = root.ReplaceNode (firstClass, newClass);

			SyntaxTree = newRoot.SyntaxTree;
		}
		void Write (ObjCMethodDecl objcMethod)
		{
			if (objcMethod.Attrs.ToList ().FirstOrDefault (a => a.Kind == CX_AttrKind.CX_AttrKind_IBAction) is null)
				return;

			var root = (CompilationUnitSyntax) SyntaxTree!.GetRoot (); // TODO: Add null checks

			var firstClass = root.DescendantNodes ().OfType<ClassDeclarationSyntax> ().First ();

			var methodName = objcMethod.Name.Replace (":", string.Empty); // TODO: Need to properly convert this to a valid C# method name

			var newMethod = MethodDeclaration (ParseTypeName ("void"), methodName)
				.AddModifiers (Token (SyntaxKind.PartialKeyword))
				.AddParameterListParameters (
					Parameter (Identifier ("sender"))
						.WithType (ParseTypeName ("Foundation.NSObject")))
				.AddAttributeLists (
					AttributeList (SingletonSeparatedList (
						Attribute (IdentifierName ("Action"),
							AttributeArgumentList (SingletonSeparatedList (
								AttributeArgument (LiteralExpression (SyntaxKind.StringLiteralExpression, Literal ($"{methodName}:")))))))))
				.WithSemicolonToken (Token (SyntaxKind.SemicolonToken));

			var newClass = firstClass.AddMembers (newMethod);

			var newRoot = root.ReplaceNode (firstClass, newClass);

			SyntaxTree = newRoot.SyntaxTree;
		}

		protected override Task VisitAttrAsync (Attr attr)
		{
			logger.Debug ($"[{nameof (ObjCSyntaxRewriter)}] Visiting {{Kind}}", attr.KindSpelling);
			return Task.CompletedTask;
		}

		protected override Task VisitRefAsync (Ref @ref)
		{
			logger.Debug ($"[{nameof (ObjCSyntaxRewriter)}] Visiting {{Kind}}", @ref.Spelling);
			return Task.CompletedTask;
		}

		protected override Task VisitStmtAsync (Stmt stmt)
		{
			logger.Debug ($"[{nameof (ObjCSyntaxRewriter)}] Visiting {{Kind}}", stmt.Spelling);
			return Task.CompletedTask;
		}
	}

	SyntaxTree SortClassMembers (SyntaxTree syntaxTree)
	{
		var root = (CompilationUnitSyntax) syntaxTree.GetRoot ();

		var firstClass = root.DescendantNodes ().OfType<ClassDeclarationSyntax> ().First ();

		var members = firstClass.Members.ToList ();

		var orderedMembers = members
			.OrderBy (m => m, new MemberDeclarationSyntaxComparer ())
			.ToArray ();

		var newClass = firstClass.WithMembers (SyntaxFactory.List (orderedMembers));
		var newRoot = root.ReplaceNode (firstClass, newClass);

		return newRoot.SyntaxTree;
	}

	/// <summary>
	/// Comparer to sort class members in a specific order
	/// </summary>
	/// <remarks>
	/// The order is as follows:
	/// 1. Fields
	/// 2. Properties
	/// 3. Constructors
	/// 4. Partial methods
	/// 5. All other Methods
	/// Within a group, members are sorted by name alphabetically
	/// </remarks>
	class MemberDeclarationSyntaxComparer : IComparer<MemberDeclarationSyntax> {
		public int Compare (MemberDeclarationSyntax? x, MemberDeclarationSyntax? y)
		{
			return x switch {
				FieldDeclarationSyntax xField when y is FieldDeclarationSyntax yField =>
					string.CompareOrdinal (xField.Declaration.Variables.First ().Identifier.ValueText,
										   yField.Declaration.Variables.First ().Identifier.ValueText),
				FieldDeclarationSyntax => -1,
				PropertyDeclarationSyntax xProperty when y is PropertyDeclarationSyntax yProperty =>
					string.CompareOrdinal (xProperty.Identifier.ValueText, yProperty.Identifier.ValueText),
				PropertyDeclarationSyntax => y is FieldDeclarationSyntax ? 1 : -1,
				ConstructorDeclarationSyntax xConstructor when y is ConstructorDeclarationSyntax yConstructor =>
					xConstructor.ParameterList.Parameters.Count.CompareTo (yConstructor.ParameterList.Parameters.Count),
				ConstructorDeclarationSyntax => y is FieldDeclarationSyntax || y is PropertyDeclarationSyntax ? 1 : -1,
				MethodDeclarationSyntax xMethod when xMethod.Modifiers.Any (m => m.ValueText == "partial") => y switch {
					MethodDeclarationSyntax yMethod when yMethod.Modifiers.Any (m => m.ValueText == "partial") =>
						string.CompareOrdinal (xMethod.Identifier.ValueText, yMethod.Identifier.ValueText),
					_ => -1,
				},
				MethodDeclarationSyntax xMethod => y switch {
					MethodDeclarationSyntax yMethod when yMethod.Modifiers.Any (m => m.ValueText == "partial") => 1,
					_ => y is MethodDeclarationSyntax ? 0 : 1,
				},
				_ => 0,
			};
		}
	}

}
