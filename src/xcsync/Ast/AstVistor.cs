// Copyright (c) Microsoft Corporation.  All rights reserved.

using ClangSharp;

namespace xcsync;

/// <summary>
/// Represents a Vistor for the Clang AST.
/// </summary>
abstract class AstVisitor : IVisitor<Cursor> {
	/// <summary>
	/// Visits the specified node.
	/// </summary>
	/// <param name="node">The node to visit.</param>
	public virtual async Task VisitAsync (Cursor cursor)
	{
		if (cursor is Attr attr) {
			await VisitAttrAsync (attr);
		} else if (cursor is Decl decl) {
			await VisitDeclAsync (decl);
		} else if (cursor is Ref @ref) {
			await VisitRefAsync (@ref);
		} else if (cursor is Stmt stmt) {
			await VisitStmtAsync (stmt);
		}
	}

	protected abstract Task VisitDeclAsync (Decl decl);
	protected abstract Task VisitStmtAsync (Stmt stmt);
	protected abstract Task VisitRefAsync (Ref @ref);
	protected abstract Task VisitAttrAsync (Attr attr);
}
