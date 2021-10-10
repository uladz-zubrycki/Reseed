using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Ordering;
using Reseed.Rendering.Dsl;
using Reseed.Schema;

namespace Reseed.Rendering
{
	internal static class SimpleInsertActionsBuilder
	{
		public static DbActionsBuilder AppendSimpleInsertActions(
			[NotNull] this DbActionsBuilder builder,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers,
			[NotNull] SimpleInsertMode mode)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));
			if (containers == null) throw new ArgumentNullException(nameof(containers));
			if (mode == null) throw new ArgumentNullException(nameof(mode));

			return mode switch
			{
				SimpleInsertScriptMode => builder.Append(DbActionStage.Insert, InsertScriptRenderer.Render(containers)),
				SimpleInsertProcedureMode procedureMode => builder
					.Append(DbActionStage.PrepareDb,
						RenderInsertDataProcedure(procedureMode.ProcedureName, containers).Order())
					.Append(DbActionStage.Insert,
						ScriptRendererUtils.RenderExecuteProcedureScript(CommonScriptNames.ExecuteInsertSp, procedureMode.ProcedureName))
					.Append(DbActionStage.CleanupDb,
						ScriptRendererUtils.RenderDropProcedureScript(CommonScriptNames.DropInsertSp, procedureMode.ProcedureName)),
				_ => throw new NotSupportedException($"Unknown cleanup mode '{mode.GetType().Name}'")
			};
		}

		public static IReadOnlyCollection<OrderedItem<DbScript>> RenderInsertDataProcedure(
			[NotNull] ObjectName name,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (containers == null) throw new ArgumentNullException(nameof(containers));
			if (containers.Count == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(containers));

			var dropProcedure = ScriptRendererUtils.RenderDropProcedureScript(CommonScriptNames.DropInsertSp, name);
			var createProcedure = InsertScriptRenderer.Render(containers)
				.Map(t => ScriptRendererUtils.RenderCreateStoredProcedure(name, t), CommonScriptNames.CreateInsertSp);

			return OrderedItem.OrderedCollection(dropProcedure, createProcedure);
		}
	}
}