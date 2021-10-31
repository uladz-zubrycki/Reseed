using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Generation.Schema;
using Reseed.Internals.Graphs;
using Reseed.Schema;

namespace Reseed.Ordering
{
	internal static class TableOrderer
	{
		public static IReadOnlyCollection<OrderedItem<ITableContainer>> Order(
			[NotNull] IReadOnlyCollection<Table> tables,
			[NotNull] OrderedGraph<TableSchema> schemas)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (schemas == null) throw new ArgumentNullException(nameof(schemas));

			var tableByName = tables.ToDictionary(t => t.Name);
			var orderedTables =
				schemas
					.FilterShallow(s => tableByName.ContainsKey(s.Name))
					.MapShallow(s => tableByName.TryGetValue(s.Name, out var t)
						? t
						: throw new InvalidOperationException(
							$"Can't find table for schema {s.Name}, filter it out"));

			return TableMutualReferenceResolver.Resolve(orderedTables);
		}
	}
}