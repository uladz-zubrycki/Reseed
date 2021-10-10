using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Dsl.Simple;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Rendering.Cleanup;
using Reseed.Rendering.Schema;
using Reseed.Schema;

namespace Reseed.Rendering.Simple
{
	internal static class SimpleModeRenderer
	{
		public static DbActions Render(
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers,
			[NotNull] SimpleMode mode)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (containers == null) throw new ArgumentNullException(nameof(containers));
			if (mode == null) throw new ArgumentNullException(nameof(mode));

			return new DbActionsBuilder()
				.AddSimpleInsertActions(mode.InsertDefinition, containers)
				.AddCleanup(mode.CleanupDefinition, tables)
				.Build();
		}
	}
}