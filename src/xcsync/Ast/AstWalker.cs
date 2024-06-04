// Copyright (c) Microsoft Corporation.  All rights reserved.

using ClangSharp;

namespace xcsync;

/// <summary>
/// Represents a walker for the Clang AST.
/// </summary>
class AstWalker : IWalker<Cursor, AstVisitor> {
	/// <summary>
	/// Walks the Clang AST.
	/// </summary>
	/// <param name="node">The node to walk.</param>
	/// <param name="visitor">The visitor to use.</param>
	/// <param name="filter">An optional filter to apply to the nodes.</param>
	async public Task WalkAsync (Cursor node, AstVisitor visitor, Func<Cursor, bool>? filter = null)
	{
		await visitor.VisitAsync (node).ConfigureAwait (false);
		var children = node.CursorChildren;
		foreach (var child in node.CursorChildren) {
			if (filter == null || filter (child)) {
				await WalkAsync (child, visitor).ConfigureAwait (false);
			}
		}
	}
}
