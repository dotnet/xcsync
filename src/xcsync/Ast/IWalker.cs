// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace xcsync;

/// <summary>
/// Represents a walker that traverses a tree-like structure and applies a visitor to each node.
/// </summary>
/// <typeparam name="TNode">The type of the nodes in the tree.</typeparam>
/// <typeparam name="TVisitor">The type of the visitor.</typeparam>
interface IWalker<TNode, TVisitor> {
	/// <summary>
	/// Walks the specified node and applies the visitor to each node in the tree.
	/// </summary>
	/// <param name="node">The root node of the tree.</param>
	/// <param name="visitor">The visitor to apply to each node.</param>
	/// <param name="filter">An optional filter function to determine which nodes to visit.</param>
	void Walk (TNode node, TVisitor visitor, Func<TNode, bool>? filter);
}
