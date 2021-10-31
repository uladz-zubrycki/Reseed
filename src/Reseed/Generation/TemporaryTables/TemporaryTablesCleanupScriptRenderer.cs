using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Schema;
using static Reseed.Generation.ScriptRenderer;

namespace Reseed.Generation.TemporaryTables
{
	internal static class TemporaryTablesCleanupScriptRenderer
	{
		public static IReadOnlyCollection<OrderedItem<SqlScriptAction>> Render(
			[NotNull] string tempSchemaName,
			[NotNull] IReadOnlyCollection<TableSchema> tables)
		{
			var foreignKeys = tables
				.SelectMany(t => t.GetRelations())
				.ToArray();

			return new List<SqlScriptAction>()
				.AddScriptWhen(
					() => new SqlScriptAction(
						"Drop temp tables foreign keys",
						RenderDropForeignKeys(
							foreignKeys,
							true)),
					foreignKeys.Length > 0)
				.AddScript(new SqlScriptAction("Drop temp tables", RenderDropTemporaryTablesScripts(tables)))
				.AddScript(new SqlScriptAction("Drop temp schema", RenderDropTemporarySchemaScript(tempSchemaName)))
				.WithNaturalOrder()
				.ToArray();
		}

		private static string RenderDropTemporaryTablesScripts(IReadOnlyCollection<TableSchema> tables) =>
			string.Join(Environment.NewLine + Environment.NewLine,
				tables.Select(t => RenderDropTable(t.Name)));
	}
}