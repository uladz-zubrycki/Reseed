using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Configuration.Basic;
using Reseed.Generation.Cleanup;
using Reseed.Generation.Schema;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Schema;

namespace Reseed.Generation.Basic
{
	internal static class BasicActionGenerator
	{
		public static SeedActions Generate(
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers,
			[NotNull] BasicSeedMode mode)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (containers == null) throw new ArgumentNullException(nameof(containers));
			if (mode == null) throw new ArgumentNullException(nameof(mode));

			return new SeedActionsBuilder()
				.AddBasicInsertActions(mode.InsertDefinition, containers)
				.AddCleanupActions(mode.CleanupDefinition, tables)
				.Build();
		}
	}
}