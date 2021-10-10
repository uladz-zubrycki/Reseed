using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Schema;
using static Reseed.Ordering.OrderedItem;
using static Reseed.Rendering.Scripts;

namespace Reseed.Rendering.TemporaryTables
{
	internal static class TemporaryTablesCleanupScriptRenderer
	{
		public static IReadOnlyCollection<OrderedItem<DbScript>> Render(
			[NotNull] string tempSchemaName,
			[NotNull] IReadOnlyCollection<TableSchema> tables) =>
			OrderedCollection(
				new DbScript(
					"Drop temp tables foreign keys",
					RenderDropForeignKeys(
						tables.SelectMany(t => t.GetRelations()),
						true)),
				new DbScript("Drop temp tables", RenderDropTables(tables)),
				new DbScript("Drop temp schema", RenderDropSchema(tempSchemaName)));

		private static string RenderDropTables(IReadOnlyCollection<TableSchema> tables) =>
			string.Join(Environment.NewLine + Environment.NewLine,
				tables.Select(t => RenderDropTable(t.Name)));
	}
}