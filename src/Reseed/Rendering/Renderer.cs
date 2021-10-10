using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Rendering.Dsl;
using Reseed.Schema;

namespace Reseed.Rendering
{
	internal static class Renderer
	{
		public static DbActions Render(
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