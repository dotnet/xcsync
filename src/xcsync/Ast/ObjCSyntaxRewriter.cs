// Copyright (c) Microsoft Corporation.  All rights reserved.

using ClangSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Serilog;
using static ClangSharp.Interop.CX_DeclKind;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace xcsync.Ast;

class ObjCSyntaxRewriter(ILogger Logger) : AstWalker { 

	internal async Task<SyntaxTree?> WriteAsync (ObjCInterfaceDecl objcType, SyntaxTree? syntaxTree)
	{
		var visitor = new Visitor(Logger, syntaxTree);
		await WalkAsync (objcType, visitor);
		return visitor.SyntaxTree;
	}

	class Visitor(ILogger Logger, SyntaxTree? syntaxTree) : AstVisitor {

		public SyntaxTree? SyntaxTree { get; private set; } = syntaxTree;

		protected override Task VisitAttrAsync (Attr attr)
		{
			Logger.Debug ($"[{nameof (ObjCSyntaxRewriter)}] Visiting {{Kind}}", attr.KindSpelling);
			return Task.CompletedTask;
		}

		protected override Task VisitDeclAsync (Decl decl)
		{
			Logger.Debug ($"[{nameof(ObjCSyntaxRewriter)}] Visiting {{Kind}}", decl.DeclKindName);
			switch (decl.Kind) {
				case CX_DeclKind_ObjCInterface:
					var objcImpl = decl as ObjCInterfaceDecl;
					Write(objcImpl!);
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

				
			// // Add the class declaration to a namespace
			// var namespaceDeclaration = NamespaceDeclaration (ParseName ("YourNamespace"))
			// 	.AddMembers (classDeclaration);

			// Create a compilation unit with the namespace
			var compilationUnit = CompilationUnit ()
				.AddMembers (classDeclaration)
				.NormalizeWhitespace ();

			// Create a new syntax tree with the compilation unit
			SyntaxTree = SyntaxTree (compilationUnit);
		}

		protected override Task VisitRefAsync (Ref @ref)
		{
			Logger.Debug ($"[{nameof (ObjCSyntaxRewriter)}] Visiting {{Kind}}", @ref.Spelling);
			return Task.CompletedTask;
		}

		protected override Task VisitStmtAsync (Stmt stmt)
		{
			Logger.Debug ($"[{nameof (ObjCSyntaxRewriter)}] Visiting {{Kind}}", stmt.Spelling);
			return Task.CompletedTask;
		}
	}
}