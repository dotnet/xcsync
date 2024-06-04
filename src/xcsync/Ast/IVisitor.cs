// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace xcsync;

/// <summary>
/// Represents a visitor that can visit nodes of type <typeparamref name="N"/>.
/// </summary>
/// <typeparam name="TNode">The type of nodes that can be visited.</typeparam>
interface IVisitor<TNode> {
	/// <summary>
	/// Visits the specified node.
	/// </summary>
	/// <param name="node">The node to visit.</param>
	void Visit (TNode node) => VisitAsync (node).Wait ();

	/// <summary>
	/// Visits the specified node.
	/// </summary>
	/// <param name="node">The node to visit.</param>
	Task VisitAsync (TNode node);
}
