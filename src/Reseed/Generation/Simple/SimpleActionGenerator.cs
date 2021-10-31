using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Configuration.Simple;
using Reseed.Generation.Cleanup;
using Reseed.Generation.Schema;
using Reseed.Internals.Graphs;
using Reseed.Ordering;
using Reseed.Schema;

namespace Reseed.Generation.Simple
{
	internal static class SimpleActionGenerator
	{
		public static SeedActions Generate(
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers,
			[NotNull] SimpleMode mode)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (containers == null) throw new ArgumentNullException(nameof(containers));
			if (mode == null) throw new ArgumentNullException(nameof(mode));

			return new SeedActionsBuilder()
				.AddSimpleInsertActions(mode.InsertDefinition, containers)
				.AddCleanupActions(mode.CleanupDefinition, tables)
				.Build();
		}
	}
}