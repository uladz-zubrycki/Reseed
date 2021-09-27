using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Rendering;
using Reseed.Schema;
using static Reseed.Rendering.Internals.DeleteScriptRenderer;
using static Reseed.Rendering.Internals.ScriptRendererUtils;
using static Reseed.Rendering.Internals.ScriptModeRenderer;
using static Reseed.Rendering.Internals.StoredProcedureModeRenderer;
using static Reseed.Rendering.Internals.TempTableModeRenderer;
using static Reseed.Rendering.Internals.CommonScriptNames;

namespace Reseed
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
				ScriptMode scriptMode => RenderScriptMode(tables, containers, scriptMode),
				StoredProcedureMode storedProcedureMode => RenderStoredProcedureMode(
					tables,
					containers,
					storedProcedureMode),
				TempTableMode tempTableMode => RenderTempTableMode(tables, containers, tempTableMode),
				_ => throw new ArgumentOutOfRangeException(nameof(mode))
			};
		}

		private static DbActions RenderScriptMode(
			OrderedGraph<TableSchema> tables,
			IReadOnlyCollection<OrderedItem<ITableContainer>> containers,
			ScriptMode options) =>
			new DbActionsBuilder()
				.Append(DbActionStage.Insert, RenderInsertData(containers))
				.Append(DbActionStage.Delete, RenderDeleteScripts(
					tables,
					options.CleanupOptions).Order())
				.Build();

		private static DbActions RenderStoredProcedureMode(
			OrderedGraph<TableSchema> tables,
			IReadOnlyCollection<OrderedItem<ITableContainer>> containers,
			StoredProcedureMode options) =>
			new DbActionsBuilder()
				.Append(DbActionStage.Insert, RenderInsertDataProcedure(
					options.InsertProcedureName,
					containers).Order())
				.Append(DbActionStage.Insert, RenderExecuteProcedureScript(
					ExecuteInsertSp,
					options.InsertProcedureName))
				.Append(DbActionStage.Delete, RenderDeleteDataProcedure(
					options.DeleteProcedureName,
					tables,
					options.CleanupOptions).Order())
				.Append(DbActionStage.CleanupDb,
					RenderDropProcedureScript(
						DropInsertSp,
						options.InsertProcedureName),
					RenderDropProcedureScript(
						DropDeleteSp,
						options.DeleteProcedureName))
				.Build();

		private static DbActions RenderTempTableMode(
			[NotNull] OrderedGraph<TableSchema> tables,
			IReadOnlyCollection<OrderedItem<ITableContainer>> containers,
			TempTableMode options)
		{
			var deleteDataProcedure = DbScript
				.Join(DeleteScript,
					RenderDeleteScripts(
							tables,
							options.CleanupOptions)
						.Order())
				.Map(s => RenderCreateStoredProcedure(options.DeleteProcedureName, s),
					CreateDeleteSp);

			var dropDeleteDataProcedure = RenderDropProcedureScript(
				DropDeleteSp,
				options.DeleteProcedureName);

			return new DbActionsBuilder()
				.Append(DbActionStage.PrepareDb, RenderInitTempTables(
					options.TempSchemaName,
					tables,
					containers).Order())
				.Append(DbActionStage.PrepareDb,
					dropDeleteDataProcedure,
					deleteDataProcedure)
				.Append(RenderInsertFromTempTables(
					options.InsertOptions,
					options.TempSchemaName,
					tables,
					containers))
				.Append(DbActionStage.Delete, RenderExecuteProcedureScript(
					ExecuteDeleteSp,
					options.DeleteProcedureName))
				.Append(DbActionStage.CleanupDb, dropDeleteDataProcedure)
				.Append(DbActionStage.CleanupDb, RenderDropTempTables(
					options.TempSchemaName,
					tables,
					containers).Order())
				.Build();
		}
	}
}