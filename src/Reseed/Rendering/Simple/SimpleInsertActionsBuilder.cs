using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Dsl.Simple;
using Reseed.Ordering;
using Reseed.Rendering.Insertion;
using Reseed.Rendering.Schema;
using Reseed.Schema;
using static Reseed.Rendering.Scripts;

namespace Reseed.Rendering.Simple
{
	internal static class SimpleInsertActionsBuilder
	{
		public static DbActionsBuilder AddSimpleInsertActions(
			[NotNull] this DbActionsBuilder builder,
			[NotNull] SimpleInsertDefinition definition,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));
			if (containers == null) throw new ArgumentNullException(nameof(containers));
			if (definition == null) throw new ArgumentNullException(nameof(definition));

			return definition switch
			{
				SimpleInsertScriptDefinition => builder.Add(DbActionStage.Insert, InsertScriptRenderer.Render(containers)),
				
				SimpleInsertProcedureDefinition procedureMode => builder
					.Add(DbActionStage.PrepareDb, RenderCreateProcedureScripts(procedureMode.ProcedureName, containers))
					.Add(DbActionStage.Insert,
						RenderExecuteProcedureScript(CommonScriptNames.ExecuteInsertSp, procedureMode.ProcedureName))
					.Add(DbActionStage.CleanupDb,
						RenderDropProcedureScript(CommonScriptNames.DropInsertSp, procedureMode.ProcedureName)),
				
				_ => throw new NotSupportedException($"Unknown cleanup mode '{definition.GetType().Name}'")
			};
		}

		private static IReadOnlyCollection<OrderedItem<DbScript>> RenderCreateProcedureScripts(
			[NotNull] ObjectName name,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers)
		{
			var dropProcedure = RenderDropProcedureScript(CommonScriptNames.DropInsertSp, name);
			var createProcedure = InsertScriptRenderer.Render(containers)
				.Map(t => RenderCreateStoredProcedure(name, t), CommonScriptNames.CreateInsertSp);

			return OrderedItem.OrderedCollection(dropProcedure, createProcedure);
		}
	}
}