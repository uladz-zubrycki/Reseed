using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Internals.Utils;
using Reseed.Ordering;

namespace Reseed.Internals.Graphs
{
	internal static class NodeOrderer<T> where T : class, INode<T>
	{
		public static OrderedGraph<T> Order([NotNull] IReadOnlyCollection<T> nodes)
		{
			if (nodes == null) throw new ArgumentNullException(nameof(nodes));
			return nodes.Count switch
			{
				0 => OrderedGraph<T>.Empty,
				_ => OrderNodes(nodes)
			};

			static OrderedGraph<T> OrderNodes(IReadOnlyCollection<T> nodes)
			{
				var processedNodes = new Dictionary<T, OrderedItem<T>>();
				var mutualReferences = new List<MutualReference<T>>();
				var _ = IterateNodes(
					nodes,
					0,
					null,
					processedNodes,
					mutualReferences,
					Fn.Identity<T>(),
					(__, n) => new ReferencePath<T>(n));

				return new OrderedGraph<T>(processedNodes.Values, mutualReferences);
			}
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
			var nextOrder = initialOrder;
			foreach (var nodeContainer in nodeContainers)
			{
				var orderedNode = GetNodeOrder(
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
			if (processedNodes.TryGetValue(node, out var processed))
			{
				return processed;
			}

			var (cyclicReferences, normalReferences) =
				node.References.PartitionBy(t => path.Contains(t.Target));

			mutualReferences.AddRange(cyclicReferences.Select(
				r => new MutualReference<T>(
					new ReferencePath<T>(node, new[] { r }),
					path.StartFrom(r.Target))));

			if (normalReferences.Length == 0)
			{
				return TrackProcessed(new OrderedItem<T>(order, node));
			}

			var nextOrder = IterateNodes(
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