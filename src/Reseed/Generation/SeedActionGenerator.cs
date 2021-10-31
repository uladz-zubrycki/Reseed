using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Configuration;
using Reseed.Configuration.Simple;
using Reseed.Configuration.TemporaryTables;
using Reseed.Generation.Schema;
using Reseed.Generation.Simple;
using Reseed.Generation.TemporaryTables;
using Reseed.Internals.Graphs;
using Reseed.Ordering;
using Reseed.Schema;

namespace Reseed.Generation
{
	internal static class SeedActionGenerator
	{
		public static SeedActions Generate(
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers,
			[NotNull] SeedMode mode)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (containers == null) throw new ArgumentNullException(nameof(containers));
			if (containers.Count == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(containers));
			if (mode == null) throw new ArgumentNullException(nameof(mode));

			return mode switch
			{
				SimpleMode simpleMode => SimpleActionGenerator.Generate(
					tables, 
					containers, 
					simpleMode),

				TemporaryTablesMode temporaryTablesMode => TemporaryTablesActionGenerator.Generate(
					tables, 
					containers,
					temporaryTablesMode),

				_ => throw new ArgumentOutOfRangeException(nameof(mode))
			};
		}
	}
}