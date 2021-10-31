using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Configuration.Simple;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Rendering.Cleanup;
using Reseed.Rendering.Schema;
using Reseed.Schema;

namespace Reseed.Rendering.Simple
{
	internal static class SimpleModeRenderer
	{
		public static SeedActions Render(
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers,
			[NotNull] SimpleMode mode)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (containers == null) throw new ArgumentNullException(nameof(containers));
			if (mode == null) throw new ArgumentNullException(nameof(mode));

			return new SeedActionsBuilder()
				.AddSimpleInsertActions(mode.InsertDefinition, containers)
				.AddCleanup(mode.CleanupDefinition, tables)
				.Build();
		}
	}
}