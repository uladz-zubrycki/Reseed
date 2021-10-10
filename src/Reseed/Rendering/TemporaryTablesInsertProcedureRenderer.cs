using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Schema;

namespace Reseed.Rendering
{
	internal static class TemporaryTablesInsertProcedureRenderer
	{
		public static IReadOnlyCollection<DbStep> GenerateInsertProcedureActions(
			[NotNull] ObjectName procedureName,
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] Func<ObjectName, ObjectName> mapTableName)
		{
			var script = TemporaryTableInsertScriptRenderer.Render(
				tables,
				mapTableName);

			var insertProcedure = script
				.Map(s => ScriptRendererUtils.RenderCreateStoredProcedure(procedureName, s),
					CommonScriptNames.CreateInsertSp);

			var dropInsertProcedure = ScriptRendererUtils.RenderDropProcedureScript(
				CommonScriptNames.DropInsertSp,
				procedureName);

			return new[]
			{
				new DbStep(DbActionStage.PrepareDb, OrderedItem.OrderedCollection<IDbAction>(
					dropInsertProcedure,
					insertProcedure)),
				new DbStep(DbActionStage.Insert, OrderedItem.OrderedCollection<IDbAction>(
					ScriptRendererUtils.RenderExecuteProcedureScript(
						CommonScriptNames.ExecuteInsertSp,
						procedureName))),
				new DbStep(DbActionStage.CleanupDb, OrderedItem.OrderedCollection<IDbAction>(
					dropInsertProcedure))
			};
		}
	}
}