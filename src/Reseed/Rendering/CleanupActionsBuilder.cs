using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Rendering.Internals;
using Reseed.Rendering.Modes;
using Reseed.Schema;

namespace Reseed.Rendering
{
	internal static class CleanupActionsBuilder
	{
		public static DbActionsBuilder AppendCleanupActions(
			[NotNull] this DbActionsBuilder builder,
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] CleanupMode mode)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (mode == null) throw new ArgumentNullException(nameof(mode));

			return mode switch
			{
				CleanupScriptMode scriptMode => builder
					.Append(DbActionStage.Delete,
						DeleteScriptRenderer.RenderDeleteScripts(tables, scriptMode.Options).Order()),

				CleanupProcedureMode procedureMode => builder
					.Append(DbActionStage.PrepareDb,
						RenderDeleteDataProcedure(procedureMode.ProcedureName, tables, procedureMode.Options).Order())
					.Append(DbActionStage.Insert,
						ScriptRendererUtils.RenderExecuteProcedureScript(CommonScriptNames.ExecuteDeleteSp,
							procedureMode.ProcedureName))
					.Append(DbActionStage.CleanupDb,
						ScriptRendererUtils.RenderDropProcedureScript(CommonScriptNames.DropDeleteSp,
							procedureMode.ProcedureName)),

				_ => throw new NotSupportedException($"Unknown cleanup mode '{mode.GetType().Name}'")
			};
		}

		private static IReadOnlyCollection<OrderedItem<DbScript>> RenderDeleteDataProcedure(
			[NotNull] ObjectName name,
			[NotNull] OrderedGraph<TableSchema> schemas,
			[NotNull] CleanupOptions options)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (schemas == null) throw new ArgumentNullException(nameof(schemas));
			if (options == null) throw new ArgumentNullException(nameof(options));

			var dropProcedure = ScriptRendererUtils.RenderDropProcedureScript(CommonScriptNames.DropDeleteSp, name);
			var createProcedure = DbScript
				.Join(CommonScriptNames.DeleteScript,
					DeleteScriptRenderer.RenderDeleteScripts(schemas, options).Order())
				.Map(s => ScriptRendererUtils.RenderCreateStoredProcedure(name, s), CommonScriptNames.CreateDeleteSp);

			return OrderedItem.OrderedCollection(dropProcedure, createProcedure);
		}
	}
}