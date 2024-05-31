// Copyright (c) Microsoft Corporation.  All rights reserved.

using ClangSharp;

namespace xcsync;

/// <summary>
/// Represents a walker for the Clang AST.
/// </summary>
class AstWalker : IWalker<Cursor, IVisitor<Cursor>>
{
	/// <summary>
	/// Walks the Clang AST.
	/// </summary>
	/// <param name="node">The node to walk.</param>
	/// <param name="visitor">The visitor to use.</param>
	/// <param name="filter">An optional filter to apply to the nodes.</param>
	public void Walk(Cursor node, IVisitor<Cursor> visitor, Func<Cursor, bool>? filter = null)
	{
		visitor.Visit(node);
		var children = node.CursorChildren;
		foreach (var child in node.CursorChildren)
		{
			if (filter == null || filter(child))
			{
				Walk(child, visitor);
			}
		}
	}
}
