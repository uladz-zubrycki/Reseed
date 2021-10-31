using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Ordering;
using Reseed.Schema;
using static Reseed.Ordering.OrderedItem;

namespace Reseed.Internals.Graphs
{
	internal static class MutualReferenceResolver
	{
		public static OrderedItem<TOut>[] MergeChunks<T, TOut>(
			[NotNull] OrderedGraph<T> graph,
			[NotNull] Func<IReadOnlyCollection<OrderedItem<T>>, TOut> mergeUsual,
			[NotNull] Func<MutualGroup<T>, TOut> mergeMutual,
			MutualGroupOrderMode mutualOrderMode = MutualGroupOrderMode.Max) where T : class
		{
			if (graph == null) throw new ArgumentNullException(nameof(graph));
			if (mergeUsual == null) throw new ArgumentNullException(nameof(mergeUsual));
			if (mergeMutual == null) throw new ArgumentNullException(nameof(mergeMutual));
			
			var orderMap = graph.Nodes.ToDictionary(o => o.Value);
			var groups = BuildMutualGroups(graph.MutualReferences, r => orderMap[r]);

			return Enumerate().ToArray();

			IEnumerable<OrderedItem<TOut>> Enumerate()
			{
				var current = new List<OrderedItem<T>>();
				var i = 0;

				foreach (var node in graph.Nodes.OrderBy(o => o.Order))
				{
					var mutualGroup = groups.FirstOrDefault(gr => gr.Contains(node.Value));
					if (mutualGroup != null)
					{
						var groupOrder = mutualOrderMode switch
						{
							MutualGroupOrderMode.Min => mutualGroup.MinOrder,
							MutualGroupOrderMode.Max => mutualGroup.MaxOrder,
							_ => throw new NotSupportedException(
								$"Unknown {nameof(MutualGroupOrderMode)} value '{mutualOrderMode}'")
						};

						if (groupOrder == node.Order)
						{
							if (current.Count > 0)
							{
								yield return Ordered(i, mergeUsual(current));
								current = new List<OrderedItem<T>>();
								i++;
							}

							yield return Ordered(i, mergeMutual(mutualGroup));
							i++;
						}
					}
					else
					{
						current.Add(node);
					}
				}

				if (current.Count > 0)
				{
					yield return Ordered(i, mergeUsual(current));
				}
			}
		}

		private static MutualGroup<T>[] BuildMutualGroups<T>(
			IReadOnlyCollection<MutualReference<T>> references,
			Func<T, OrderedItem<T>> getOrdered) where T : class
		{
			var itemSets = new List<HashSet<T>>();
			var relationMap = new Dictionary<HashSet<T>, List<Relation<T>>>();

			foreach (var reference in references)
			{
				var set = itemSets.FirstOrDefault(s => reference.Items.Any(s.Contains));
				if (set == null)
				{
					var newSet = new HashSet<T>(reference.Items);
					itemSets.Add(newSet);
					relationMap.Add(newSet, reference.Relations.ToList());
				}
				else
				{
					foreach (var item in reference.Items)
					{
						set.Add(item);
					}
					relationMap[set].AddRange(reference.Relations);
				}
			}

			return itemSets
				.Select(s =>
				{
					var items = s.Select(getOrdered).ToArray();
					return new MutualGroup<T>(
						items,
						relationMap[s]
							.Distinct()
							.ToArray());
				})
				.ToArray();
		}
	}

	internal enum MutualGroupOrderMode
	{
		/// <summary>
		/// Merges mutually referenced items into one,
		/// as if it was the only item having order, which is minimal for the group. 
		/// </summary>
		Min,

		/// <summary>
		/// Merges mutually referenced items into one,
		/// as if it was the only item having order, which is minimal for the group.
		/// </summary>
		Max
	}
}