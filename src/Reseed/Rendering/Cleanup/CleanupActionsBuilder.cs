using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Dsl.Cleanup;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Schema;
using static Reseed.Rendering.Scripts;

namespace Reseed.Rendering.Cleanup
{
	internal static class CleanupActionsBuilder
	{
		public static SeedActionsBuilder AddCleanup(
			[NotNull] this SeedActionsBuilder builder,
			[NotNull] CleanupDefinition definition,
			[NotNull] OrderedGraph<TableSchema> tables)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (definition == null) throw new ArgumentNullException(nameof(definition));

			return definition switch
			{
				CleanupScriptDefinition scriptDefinition => builder
					.Add(SeedStage.Delete, DeleteScriptRenderer.Render(tables, scriptDefinition.Options)),

				CleanupProcedureDefinition procedureDefinition => builder
					.Add(SeedStage.PrepareDb, RenderCreateProcedureScripts(
						procedureDefinition.ProcedureName, tables, procedureDefinition.Options))
					.Add(SeedStage.Delete, RenderExecuteProcedureScript(
						CommonScriptNames.ExecuteDeleteSp, procedureDefinition.ProcedureName))
					.Add(SeedStage.CleanupDb, RenderDropProcedureScript(
						CommonScriptNames.DropDeleteSp, procedureDefinition.ProcedureName)),

				_ => throw new NotSupportedException($"Unknown cleanup mode '{definition.GetType().Name}'")
			};
		}

		private static IReadOnlyCollection<OrderedItem<SqlScriptAction>> RenderCreateProcedureScripts(
			[NotNull] ObjectName name,
			[NotNull] OrderedGraph<TableSchema> schemas,
			[NotNull] CleanupOptions options)
		{
			var dropProcedure = RenderDropProcedureScript(CommonScriptNames.DropDeleteSp, name);
			var createProcedure = SqlScriptAction
				.Join(CommonScriptNames.DeleteScript, DeleteScriptRenderer.Render(schemas, options).Order())
				.Map(s => RenderCreateStoredProcedure(name, s), CommonScriptNames.CreateDeleteSp);

			return OrderedItem.OrderedCollection(dropProcedure, createProcedure);
		}
	}
}