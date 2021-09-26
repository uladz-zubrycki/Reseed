using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Ordering;
using Reseed.Utils;
using Testing.Common.Api.Schema;

namespace Reseed.Graphs
{
	internal static class NodeOrderer<T> where T : class, INode<T>
	{
		public static OrderedGraph<T> Order([NotNull] IReadOnlyCollection<T> nodes)
		{
			if (nodes == null) throw new ArgumentNullException(nameof(nodes));
			if (nodes.Count == 0)
			{
				return OrderedGraph<T>.Empty;
			}

			var processedNodes = new Dictionary<T, OrderedItem<T>>();
			var mutualReferences = new List<MutualReference<T>>();
			int _ = IterateNodes(
				nodes,
				0,
				null,
				processedNodes,
				mutualReferences,
				__ => __,
				(__, n) => new ReferencePath<T>(n));

			return new OrderedGraph<T>(processedNodes.Values, mutualReferences);
		}

		private static int IterateNodes<TNode>(
			IEnumerable<TNode> nodeContainers,
			int initialOrder,
			ReferencePath<T> referencePath,
			Dictionary<T, OrderedItem<T>> processedNodes,
			List<MutualReference<T>> mutualReferences,
			Func<TNode, T> getNode,
			Func<ReferencePath<T>, TNode, ReferencePath<T>> appendPath)
		{
			int nextOrder = initialOrder;
			foreach (TNode nodeContainer in nodeContainers)
			{
				OrderedItem<T> orderedNode = GetNodeOrder(
					getNode(nodeContainer),
					nextOrder,
					appendPath(referencePath, nodeContainer),
					processedNodes,
					mutualReferences);

				nextOrder = orderedNode.Order >= nextOrder
					? orderedNode.Order + 1
					: nextOrder;
			}

			return nextOrder;
		}

		private static OrderedItem<T> GetNodeOrder(
			T node,
			int order,
			ReferencePath<T> path,
			Dictionary<T, OrderedItem<T>> processedNodes,
			List<MutualReference<T>> mutualReferences)
		{
			if (processedNodes.TryGetValue(node, out OrderedItem<T> processed))
			{
				return processed;
			}

			(Reference<T>[] cyclicReferences, Reference<T>[] normalReferences) =
				node.References.PartitionBy(t => path.Contains(t.Target));

			mutualReferences.AddRange(cyclicReferences.Select(
				r =>
					new MutualReference<T>(
						new ReferencePath<T>(node, new[] { r }),
						path.StartFrom(r.Target))));

			if (normalReferences.Length == 0)
			{
				return TrackProcessed(new OrderedItem<T>(order, node));
			}

			int nextOrder = IterateNodes(
				normalReferences,
				order,
				path,
				processedNodes,
				mutualReferences,
				r => r.Target,
				(p, r) => p.Append(r));

			return TrackProcessed(new OrderedItem<T>(nextOrder, node));

			OrderedItem<T> TrackProcessed(OrderedItem<T> orderedNode)
			{
				processedNodes.Add(orderedNode.Value, orderedNode);
				return orderedNode;
			}
		}
	}
}