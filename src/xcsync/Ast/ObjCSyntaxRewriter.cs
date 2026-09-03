// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ClangSharp;
using ClangSharp.Interop;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Serilog;
using System.Text.RegularExpressions;
using System.IO.Abstractions;
using xcsync.Projects;
using static ClangSharp.Interop.CX_DeclKind;
using static ClangSharp.Interop.CXTypeKind;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Newtonsoft.Json.Linq;

namespace xcsync.Ast;

class ObjCSyntaxRewriter (ILogger Logger, ITypeService typeService, Workspace workspace, bool explicitTypes = false) : AstWalker {
	static readonly IFileSystem FileSystem = new FileSystem ();

	static readonly string NSObjectType = "Foundation.NSObject";

	internal async Task<SyntaxTree?> WriteAsync (ObjCInterfaceDecl objcType, SyntaxTree? syntaxTree)
	{
		var visitor = new Visitor (Logger, typeService, syntaxTree, explicitTypes);
		await WalkAsync (objcType, visitor).ConfigureAwait (false);

		// Now that we have the basic tree, lets make sure it generates pretty C# code
		var sortedTree = SortClassMembers (visitor.SyntaxTree!);
		var root = sortedTree!.GetRoot ();
		// root = root.NormalizeWhitespace (eol: Environment.NewLine);

		// Add a blank line between each method, so that the generated code is easier to read
		// except for the first method, which should not have a blank line before it
		root = new MethodNewLineRewriter ().Visit (root);

		// Format the code
		root = Formatter.Format (root, workspace);

		return root.SyntaxTree;
	}

	class MethodNewLineRewriter : CSharpSyntaxRewriter {
		public override SyntaxNode? VisitClassDeclaration (ClassDeclarationSyntax node)
		{
			var methods = node.Members.OfType<MethodDeclarationSyntax> ().ToList ();
			if (methods.Count > 1) {
				var updatedMethods = new List<MethodDeclarationSyntax> {
					methods.First () // Add the first method unchanged
				};

				// Add a newline before each subsequent method
				foreach (var method in methods.Skip (1)) {
					var leadingTrivia = method.GetLeadingTrivia ();
					leadingTrivia = leadingTrivia.Insert (0, Whitespace (Environment.NewLine)).Insert (0, Whitespace (Environment.NewLine));
					updatedMethods.Add (method.WithLeadingTrivia (leadingTrivia));
				}

				// Replace the old methods with the updated ones
				foreach (var method in methods.Skip (1)) {
					node = node.ReplaceNode (method, updatedMethods.First (m => m.Identifier.ValueText == method.Identifier.ValueText));
					updatedMethods.Remove (updatedMethods.First (m => m.Identifier.ValueText == method.Identifier.ValueText));
				}
			}

			return base.VisitClassDeclaration (node);
		}
	}

	public static SyntaxTree AddNewLineBeforeMethods (SyntaxTree syntaxTree)
	{
		var rewriter = new MethodNewLineRewriter ();
		var newRoot = rewriter.Visit (syntaxTree.GetRoot ());

		return newRoot.SyntaxTree;
	}

	class Visitor (ILogger logger, ITypeService typeService, SyntaxTree? syntaxTree, bool explicitTypes = false) : AstVisitor {

		public SyntaxTree? SyntaxTree { get; private set; } = syntaxTree;

		protected override Task VisitDeclAsync (Decl decl)
		{
			logger.Debug (Strings.ObjCSyntax.Visiting (nameof (ObjCSyntaxRewriter), decl.DeclKindName));
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

			}
			;
			return Task.CompletedTask;
		}

		void Write (ObjCInterfaceDecl objcImpl)
		{
			// Create the ReleaseDesignerOutlets method
			var releaseDesignerOutletsMethod = MethodDeclaration (PredefinedType (Token (SyntaxKind.VoidKeyword)), "ReleaseDesignerOutlets")
				// .AddModifiers (Token (SyntaxKind.PublicKeyword))
				.WithBody (Block ());

			var leadingTrivia = SyntaxTree?.GetRoot ().DescendantNodes ().OfType<ClassDeclarationSyntax> ().First ().GetLeadingTrivia () ?? new SyntaxTriviaList (Whitespace (Environment.NewLine));
			var trailingTrivia = SyntaxTree?.GetRoot ().DescendantNodes ().OfType<ClassDeclarationSyntax> ().First ().GetTrailingTrivia ();

			// Create a new class declaration
			var classDeclaration = ClassDeclaration (objcImpl.Name)
				.WithLeadingTrivia (leadingTrivia)
				.WithTrailingTrivia (trailingTrivia)
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
			string propertyName = objcProperty.Name;
			string? propertyTypeObjC;
			if (objcProperty.Attrs.ToList ().FirstOrDefault (a => a.Kind == CX_AttrKind.CX_AttrKind_IBOutlet) is not null) {
				propertyTypeObjC = GetObjCTypeName (objcProperty.Type.AsString);
			}
			// Determine the Objective-C type for the property.
			// If the property has the IBOutlet attribute, we can get its type directly.
			// Note: Some properties may not have the IBOutlet attribute (e.g., due to a missing import in the .h file).
			// In such cases, attempt to parse the type manually from the header file. This can occur when wiring
			// UI components in Xcode, XCode then doesn't add the import command.
			else {
				propertyTypeObjC = TryGetOutletPropertyTypeFromSource (objcProperty);
			}			

			if(objcProperty.Type.Kind != CXType_ObjCObjectPointer && objcProperty.Type.Kind != CXType_Pointer) {
				throw new NotImplementedException (Strings.ObjCSyntax.PropertyNotImplementedException (objcProperty.Type.KindSpelling));
			}

			var root = (CompilationUnitSyntax) SyntaxTree!.GetRoot ();
			var firstClass = root.DescendantNodes ().OfType<ClassDeclarationSyntax> ().First ();

			logger.Debug (Strings.ObjCSyntax.ParsingProperty (nameof (ObjCSyntaxRewriter), objcProperty.Type.AsString));

			var mappedType = MapObjectCType (propertyTypeObjC, firstClass);

			// Create the property
			var property = PropertyDeclaration (ParseTypeName (mappedType), propertyName)
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
				var disposeStatement = ParseStatement ($"if ({propertyName} != null)\n{{\n{propertyName}.Dispose ();\n {propertyName} = null;\n }}");
				var newReleaseDesignerOutletsMethod = releaseDesignerOutletsMethod.AddBodyStatements (disposeStatement);
				members [members.IndexOf (releaseDesignerOutletsMethod)] = newReleaseDesignerOutletsMethod;
			}

			var newClass = firstClass.WithMembers (List (members));

			var newRoot = root.ReplaceNode (firstClass, newClass);

			SyntaxTree = newRoot.SyntaxTree;
		}

		string MapObjectCType (string? type, ClassDeclarationSyntax classDeclaration)
		{
			if (string.IsNullOrEmpty (type)) {
				return NSObjectType;
			}

			var typeMapping = typeService.QueryTypes (null, type).FirstOrDefault ();
			if(typeMapping is null) {
				return NSObjectType;
			}

			// This method converts a TypeMapping into a usable CLR type name,
			// taking namespaces into account to avoid ambiguity.

			// If the type has no namespace or is in the global namespace,
			// we can safely return just the CLR type name.
			if (typeMapping.TypeSymbol?.ContainingNamespace is null || typeMapping.TypeSymbol.ContainingNamespace.IsGlobalNamespace)
				return typeMapping.ClrType;

			// Get the namespace of the resolved type (e.g. "MyProject.Models")
			var typeNamespace = typeMapping.TypeSymbol.ContainingNamespace.ToDisplayString ();
			var classNamespace = classDeclaration.Ancestors ().OfType<BaseNamespaceDeclarationSyntax> ().FirstOrDefault ()?.Name.ToString ();

			// If the class is in the same namespace as the type, we can return just the CLR type name.
			return classNamespace == typeNamespace
				? typeMapping.ClrType
				: $"{typeNamespace}.{typeMapping.ClrType}";
		}

		void Write (ObjCMethodDecl objcMethod)
		{
			if (objcMethod.Attrs.ToList ().FirstOrDefault (a => a.Kind == CX_AttrKind.CX_AttrKind_IBAction) is null)
				return;

			var root = (CompilationUnitSyntax) SyntaxTree!.GetRoot (); // TODO: Add null checks

			var firstClass = root.DescendantNodes ().OfType<ClassDeclarationSyntax> ().First ();

			var methodName = objcMethod.Name.Replace (":", string.Empty); // TODO: Need to properly convert this to a valid C# method name
			var senderType = explicitTypes ? GetActionParameterTypeName (objcMethod, firstClass) : NSObjectType;

			var newMethod = MethodDeclaration (ParseTypeName ("void"), methodName)
				.AddModifiers (Token (SyntaxKind.PartialKeyword))
				.AddParameterListParameters (
					Parameter (Identifier ("sender"))
						.WithType (ParseTypeName (senderType)))
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

		string GetActionParameterTypeName (ObjCMethodDecl objcMethod, ClassDeclarationSyntax classDeclaration)
		{
			var actionParameter = objcMethod.Parameters.FirstOrDefault ();

			if (actionParameter is null)
				return NSObjectType;

			string? actionParameterType;
			try {
				actionParameterType = actionParameter.OriginalType.AsString;
			} catch (ArgumentOutOfRangeException) {
				actionParameterType = TryGetActionParameterTypeFromSource (objcMethod);
			}

			if (string.IsNullOrEmpty (actionParameterType) || actionParameterType == "id")
				return NSObjectType;

			var objcTypeName = actionParameterType.Split (' ') [0];
			return MapObjectCType (objcTypeName, classDeclaration);
		}

		static string? GetObjCTypeName (string? objcType)
		{
			if (string.IsNullOrWhiteSpace (objcType))
				return null;

			var match = Regex.Match (objcType, @"(?<type>[A-Za-z_]\w*)");
			return match.Success ? match.Groups ["type"].Value : null;
		}

		static string? GetSourceContent (CXSourceRange sourceRange)
		{
			sourceRange.Start.GetFileLocation (out var file, out _, out _, out var start);
			sourceRange.End.GetFileLocation (out _, out _, out _, out var end);

			var fileContent = FileSystem.File.ReadAllText (file.Name.CString);

			return fileContent.Substring ((int) start, (int) (end - start)).Trim ();
		}

		static string? TryGetOutletPropertyTypeFromSource (ObjCPropertyDecl objcProperty)
		{			
			var source = GetSourceContent (objcProperty.Extent);
			if (string.IsNullOrEmpty (source)) 
				return null;

			var match = Regex.Match (source, $@"IBOutlet\s*(?<type>\w*)\s*\*\s*{Regex.Escape (objcProperty.Name)}", RegexOptions.IgnoreCase);

			return match.Success ? match.Groups ["type"].Value.Trim () : null;
		}

		static string? TryGetActionParameterTypeFromSource (ObjCMethodDecl objcMethod)
		{
			var source = GetSourceContent (objcMethod.Extent);
			if (string.IsNullOrEmpty (source))
				return null;

			var match = Regex.Match (source, @":\(([^)]+)\)\s*\w+");

			return match.Success ? match.Groups [1].Value.Trim () : null;
		}

		protected override Task VisitAttrAsync (Attr attr)
		{
			logger.Debug (Strings.ObjCSyntax.Visiting (nameof (ObjCSyntaxRewriter), attr.KindSpelling));
			return Task.CompletedTask;
		}

		protected override Task VisitRefAsync (Ref @ref)
		{
			logger.Debug (Strings.ObjCSyntax.Visiting (nameof (ObjCSyntaxRewriter), @ref.Spelling));
			return Task.CompletedTask;
		}

		protected override Task VisitStmtAsync (Stmt stmt)
		{
			logger.Debug (Strings.ObjCSyntax.Visiting (nameof (ObjCSyntaxRewriter), stmt.Spelling));
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

		var newClass = firstClass.WithMembers (List (orderedMembers));
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
