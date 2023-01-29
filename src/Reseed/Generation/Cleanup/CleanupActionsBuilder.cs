using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Configuration.Cleanup;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Schema;
using static Reseed.Generation.ScriptRenderer;

namespace Reseed.Generation.Cleanup
{
	internal static class CleanupActionsBuilder
	{
		public static SeedActionsBuilder AddCleanupActions(
			[NotNull] this SeedActionsBuilder builder,
			[NotNull] AnyCleanupDefinition definition,
			[NotNull] OrderedGraph<TableSchema> tables)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (definition == null) throw new ArgumentNullException(nameof(definition));

			return definition switch
			{
				EmptyCleanupDefinition => builder,
				
				CleanupScriptDefinition scriptDefinition => builder
					.Add(SeedStage.Delete, DeleteScriptRenderer.Render(tables, scriptDefinition.Configuration))
					.Add(SeedStage.CleanupDb, DeleteScriptRenderer.Render(tables,scriptDefinition.Configuration)),

				CleanupProcedureDefinition procedureDefinition => builder
					.Add(SeedStage.PrepareDb, RenderCreateProcedureScripts(
						procedureDefinition.ProcedureName, tables, procedureDefinition.Configuration))
					.Add(SeedStage.Delete, RenderExecuteProcedureScript(
						ScriptNames.ExecuteDeleteSp, procedureDefinition.ProcedureName))
					.Add(SeedStage.CleanupDb, RenderDropProcedureScript(
						ScriptNames.DropDeleteSp, procedureDefinition.ProcedureName)),

				_ => throw new NotSupportedException(
					$"Unknown {nameof(CleanupDefinition)} '{definition.GetType().Name}'")
			};
		}

		private static IReadOnlyCollection<OrderedItem<SqlScriptAction>> RenderCreateProcedureScripts(
			[NotNull] ObjectName name,
			[NotNull] OrderedGraph<TableSchema> schemas,
			[NotNull] CleanupConfiguration configuration)
		{
			var dropProcedure = RenderDropProcedureScript(ScriptNames.DropDeleteSp, name);
			var createProcedure = SqlScriptAction
				.Join(ScriptNames.DeleteScript, DeleteScriptRenderer.Render(schemas, configuration))
				.Map(s => RenderCreateStoredProcedure(name, s), ScriptNames.CreateDeleteSp);

			return OrderedItem.OrderedCollection(dropProcedure, createProcedure);
		}
	}
}