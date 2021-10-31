using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Schema;
using Testing.Common.Api.Schema;

namespace Reseed.Internals.Graphs
{
	internal interface INode<T> where T : INode<T>
	{
		IReadOnlyCollection<Reference<T>> References { get; }
		T With(IReadOnlyCollection<Reference<T>> references);
	}

	internal static class NodeExtensions
	{
		public static IReadOnlyCollection<Relation<T>> GetRelations<T>(
			[NotNull] this T node) where T : INode<T>
		{
			if (node == null) throw new ArgumentNullException(nameof(node));

			return node.References
				.Select(r => new Relation<T>(node, r.Target, r.Association))
				.ToArray();
		}

		public static IReadOnlyCollection<TOut> MapDeep<T, TOut>(
			[NotNull] this IEnumerable<T> nodes,
			[NotNull] Func<Reference<T>, TOut, Reference<TOut>> createReference,
			[NotNull] Func<T, Reference<TOut>[], TOut> createNode)
			where T : class, INode<T>
			where TOut : IMutableNode<TOut>
		{
			if (nodes == null) throw new ArgumentNullException(nameof(nodes));
			if (createNode == null) throw new ArgumentNullException(nameof(createNode));

			var ns = nodes.ToArray();
			return NodeBuilder<TOut>.CollectNodes(
				ns,
				ns.SelectMany(n => n.GetRelations()).ToArray(),
				createReference,
				createNode);
		}
	}
}