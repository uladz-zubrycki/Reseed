using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Dsl;
using Reseed.Dsl.Simple;
using Reseed.Dsl.TemporaryTables;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Rendering.Schema;
using Reseed.Rendering.Simple;
using Reseed.Rendering.TemporaryTables;
using Reseed.Schema;

namespace Reseed.Rendering
{
	internal static class Renderer
	{
		public static SeedActions Render(
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers,
			[NotNull] RenderMode mode)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (containers == null) throw new ArgumentNullException(nameof(containers));
			if (containers.Count == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(containers));
			if (mode == null) throw new ArgumentNullException(nameof(mode));

			return mode switch
			{
				SimpleMode simpleMode => SimpleModeRenderer.Render(
					tables, 
					containers, 
					simpleMode),

				TemporaryTablesMode temporaryTablesMode => TemporaryTablesModeRenderer.Render(
					tables, 
					containers,
					temporaryTablesMode),

				_ => throw new ArgumentOutOfRangeException(nameof(mode))
			};
		}
	}
}