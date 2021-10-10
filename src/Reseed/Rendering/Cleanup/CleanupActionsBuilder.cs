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
		public static DbActionsBuilder AddCleanup(
			[NotNull] this DbActionsBuilder builder,
			[NotNull] CleanupDefinition definition,
			[NotNull] OrderedGraph<TableSchema> tables)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (definition == null) throw new ArgumentNullException(nameof(definition));

			return definition switch
			{
				CleanupScriptDefinition scriptDefinition => builder
					.Add(DbActionStage.Delete, DeleteScriptRenderer.Render(tables, scriptDefinition.Options)),

				CleanupProcedureDefinition procedureDefinition => builder
					.Add(DbActionStage.PrepareDb, RenderCreateProcedureScripts(
						procedureDefinition.ProcedureName, tables, procedureDefinition.Options))
					.Add(DbActionStage.Delete, RenderExecuteProcedureScript(
						CommonScriptNames.ExecuteDeleteSp, procedureDefinition.ProcedureName))
					.Add(DbActionStage.CleanupDb, RenderDropProcedureScript(
						CommonScriptNames.DropDeleteSp, procedureDefinition.ProcedureName)),

				_ => throw new NotSupportedException($"Unknown cleanup mode '{definition.GetType().Name}'")
			};
		}

		private static IReadOnlyCollection<OrderedItem<DbScript>> RenderCreateProcedureScripts(
			[NotNull] ObjectName name,
			[NotNull] OrderedGraph<TableSchema> schemas,
			[NotNull] CleanupOptions options)
		{
			var dropProcedure = RenderDropProcedureScript(CommonScriptNames.DropDeleteSp, name);
			var createProcedure = DbScript
				.Join(CommonScriptNames.DeleteScript, DeleteScriptRenderer.Render(schemas, options).Order())
				.Map(s => RenderCreateStoredProcedure(name, s), CommonScriptNames.CreateDeleteSp);

			return OrderedItem.OrderedCollection(dropProcedure, createProcedure);
		}
	}
}