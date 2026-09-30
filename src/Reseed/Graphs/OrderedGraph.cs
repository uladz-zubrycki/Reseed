using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Ordering;
using Reseed.Schema;

namespace Reseed.Graphs
{
	internal sealed class OrderedGraph<T> where T : class
	{
		public static readonly OrderedGraph<T> Empty = new(
			Array.Empty<OrderedItem<T>>(),
			Array.Empty<MutualReference<T>>(),
			Array.Empty<Relation<T>>());

		public readonly IReadOnlyCollection<OrderedItem<T>> Nodes;
		public readonly IReadOnlyCollection<MutualReference<T>> MutualReferences;
		public readonly IReadOnlyCollection<Relation<T>> Relations;

		public int Count => Nodes.Count;

		public OrderedGraph(
			[NotNull] IReadOnlyCollection<OrderedItem<T>> nodes,
			[NotNull] IReadOnlyCollection<MutualReference<T>> mutualReferences,
			[NotNull] IReadOnlyCollection<Relation<T>> relations)
		{
			this.Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
			this.MutualReferences = mutualReferences ?? throw new ArgumentNullException(nameof(mutualReferences));
			this.Relations = relations ?? throw new ArgumentNullException(nameof(relations));
		}

		public OrderedGraph<TOut> MapShallow<TOut>([NotNull] Func<T, TOut> mapper) where TOut : class
		{
			if (mapper == null) throw new ArgumentNullException(nameof(mapper));
			return new OrderedGraph<TOut>(
				this.Nodes
					.Select(o => o.Map(mapper))
					.ToArray(),
				this.MutualReferences.Select(r => r.Map(mapper)).ToArray(),
				this.Relations.Select(r => r.Map(mapper)).ToArray());
		}

		public OrderedGraph<T> FilterShallow([NotNull] Func<T, bool> predicate) 
		{
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));
			return new OrderedGraph<T>(
				this.Nodes.Where(o => predicate(o.Value)).ToArray(),
				this.MutualReferences
					.Where(r => r.Items.All(predicate))
					.ToArray(),
				this.Relations
					.Where(r => predicate(r.Source) && predicate(r.Target))
					.ToArray());
		}

		public OrderedGraph<T> Reverse()
		{
			var maxIndex = this.Nodes.Count - 1;

			return new OrderedGraph<T>(
				this.Nodes.Select(n => n.MapOrder(i => maxIndex - i)).ToArray(),
				this.MutualReferences,
				this.Relations);
		}
	}

	internal static class OrderedGraphExtensions
	{
		public static OrderedGraph<T> FilterDeep<T>(
			[NotNull] this OrderedGraph<T> graph,
			[NotNull] Func<T, bool> predicate) 
			where T : class, IMutableNode<T>
		{
			if (graph == null) throw new ArgumentNullException(nameof(graph));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));
			
			var backMapping = new Dictionary<T, T>();
			var mapping = graph.Nodes
				.Select(o => o.Value)
				.Where(predicate)
				.MapDeep<T, T>(
					(r, n) => r.Map(_ => n),
					(n, rs) =>
					{
						var nn = n.With(rs
							.Where(r => predicate(r.Target))
							.ToArray());

						backMapping.Add(nn, n);
						return nn;
					})
				.ToDictionary(n => backMapping[n]);

			return new OrderedGraph<T>(
				graph.Nodes
					.Where(o => mapping.ContainsKey(o.Value))
					.Select(o => o.Map(n => mapping[n]))
					.ToArray(),
				graph.MutualReferences
					.Where(r => r.Items.All(mapping.ContainsKey))
					.Select(r => r.Map(n => mapping[n]))
					.ToArray(),
				graph.Relations
					.Where(r => mapping.ContainsKey(r.Source) && mapping.ContainsKey(r.Target))
					.Select(r => r.Map(n => mapping[n]))
					.ToArray());
		}
	}
}