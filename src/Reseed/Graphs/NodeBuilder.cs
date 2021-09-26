using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Schema.Internals;
using Reseed.Utils;
using Testing.Common.Api.Schema;

namespace Reseed.Graphs
{
	internal static class NodeBuilder<TOut> where TOut : IMutableNode<TOut>
	{
		public static IReadOnlyCollection<TOut> CollectNodes<T>(
			[NotNull] IReadOnlyCollection<T> items,
			[NotNull] IReadOnlyCollection<Relation<T>> relations,
			[NotNull] Func<Reference<T>, TOut, Reference<TOut>> createReference,
			[NotNull] Func<T, Reference<TOut>[], TOut> createNode)
			where T : class
		{
			if (items == null) throw new ArgumentNullException(nameof(items));
			if (relations == null) throw new ArgumentNullException(nameof(relations));
			if (createReference == null) throw new ArgumentNullException(nameof(createReference));
			if (createNode == null) throw new ArgumentNullException(nameof(createNode));

			Dictionary<T, Reference<T>[]> references = CollectReferences(items, relations);
			var cyclicReferencesMap = new Dictionary<T, Reference<T>[]>();
			var nodesMap = new Dictionary<T, TOut>();

			TOut[] nodes = references.Keys
				.Select(t => BuildNode(
					t,
					new ReferencePath<T>(t),
					references,
					cyclicReferencesMap,
					nodesMap,
					createReference,
					createNode))
				.ToArray();

			foreach (KeyValuePair<T, Reference<T>[]> kv in cyclicReferencesMap)
			{
				TOut node = nodesMap[kv.Key];
				Reference<T>[] itemReferences = kv.Value;

				node.AddReferences(
					itemReferences
						.Select(t => createReference(t, nodesMap[t.Target]))
						.ToArray());
			}

			return nodes;
		}

		private static Dictionary<T, Reference<T>[]> CollectReferences<T>(
			IEnumerable<T> items,
			IEnumerable<Relation<T>> relations)
		{
			Dictionary<T, Reference<T>[]> referencesMap =
				relations
					.GroupBy(k => k.Source)
					.ToDictionary(gr => gr.Key,
						gr => gr
							.Select(fk => new Reference<T>(fk.Association, fk.Target))
							.ToArray());

			return items.ToDictionary(
				t => t,
				t => referencesMap.TryGetValue(t, out Reference<T>[] r)
					? r
					: Array.Empty<Reference<T>>());
		}

		private static TOut BuildNode<T>(
			T item,
			ReferencePath<T> path,
			Dictionary<T, Reference<T>[]> referencesMap,
			Dictionary<T, Reference<T>[]> cyclicReferencesMap,
			Dictionary<T, TOut> nodeMap,
			Func<Reference<T>, TOut, Reference<TOut>> createReference,
			Func<T, Reference<TOut>[], TOut> createNode)
			where T : class
		{
			if (nodeMap.TryGetValue(item, out TOut existingNode))
			{
				return existingNode;
			}
			else if (referencesMap.TryGetValue(item, out Reference<T>[] rs))
			{
				(Reference<T>[] processedReferences,
						Reference<T>[] nonProcessedReferences) =
					rs.PartitionBy(r => path.Contains(r.Target));

				if (processedReferences.Length > 0)
				{
					MarkCyclicReferences(processedReferences);
				}

				Reference<TOut>[] references = nonProcessedReferences.Select(r =>
					{
						TOut node = BuildNode(
							r.Target,
							path.Append(r),
							referencesMap,
							cyclicReferencesMap,
							nodeMap,
							createReference,
							createNode);

						return createReference(r, node);
					})
					.ToArray();

				return CreateNode(references);
			}
			else
			{
				return CreateNode(Array.Empty<Reference<TOut>>());
			}

			TOut CreateNode(Reference<TOut>[] references)
			{
				TOut node = createNode(item, references);
				nodeMap.Add(item, node);
				return node;
			}

			void MarkCyclicReferences(IReadOnlyCollection<Reference<T>> references)
			{
				if (cyclicReferencesMap.TryGetValue(item, out Reference<T>[] targetReferences))
				{
					cyclicReferencesMap[item] = targetReferences.Concat(references).ToArray();
				}
				else
				{
					cyclicReferencesMap.Add(item, references.ToArray());
				}
			}
		}
	}
}