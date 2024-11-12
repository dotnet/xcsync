// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using ClangSharp;
using Serilog;

namespace xcsync;

/// <summary>
/// Represents a Vistor for the Clang AST.
/// </summary>
class ObjCImplementationDeclVisitor (ILogger Logger) : AstVisitor {
	public ObservableCollection<ObjCImplementationDecl> ObjCTypes { get; } = [];

	protected override Task VisitDeclAsync (Decl decl)
	{
		if (decl is ObjCImplementationDecl objCImplementationDecl) {
			Logger.Debug (Strings.ObjCSyntax.ObjCImplementationDeclFound (objCImplementationDecl.Name));
			ObjCTypes.Add (objCImplementationDecl);
		}
		return Task.CompletedTask;
	}

	protected override Task VisitAttrAsync (Attr attr) => Task.CompletedTask; // Nothing to do here.

	protected override Task VisitRefAsync (Ref @ref) => Task.CompletedTask; // Nothing to do here.

	protected override Task VisitStmtAsync (Stmt stmt) => Task.CompletedTask; // Nothing to do here.
}
