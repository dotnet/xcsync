// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
			await VisitAttrAsync (attr).ConfigureAwait (false);
		} else if (cursor is Decl decl) {
			await VisitDeclAsync (decl).ConfigureAwait (false);
		} else if (cursor is Ref @ref) {
			await VisitRefAsync (@ref).ConfigureAwait (false);
		} else if (cursor is Stmt stmt) {
			await VisitStmtAsync (stmt).ConfigureAwait (false);
		}
	}

	protected abstract Task VisitDeclAsync (Decl decl);
	protected abstract Task VisitStmtAsync (Stmt stmt);
	protected abstract Task VisitRefAsync (Ref @ref);
	protected abstract Task VisitAttrAsync (Attr attr);
}
