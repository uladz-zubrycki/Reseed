using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Configuration.Simple;
using Reseed.Ordering;
using Reseed.Rendering.Insertion;
using Reseed.Rendering.Schema;
using Reseed.Schema;
using static Reseed.Rendering.Scripts;

namespace Reseed.Rendering.Simple
{
	internal static class SimpleInsertActionsBuilder
	{
		public static SeedActionsBuilder AddSimpleInsertActions(
			[NotNull] this SeedActionsBuilder builder,
			[NotNull] SimpleInsertDefinition definition,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));
			if (containers == null) throw new ArgumentNullException(nameof(containers));
			if (definition == null) throw new ArgumentNullException(nameof(definition));

			return definition switch
			{
				SimpleInsertScriptDefinition => builder.Add(SeedStage.Insert, InsertScriptRenderer.Render(containers)),
				
				SimpleInsertProcedureDefinition procedureMode => builder
					.Add(SeedStage.PrepareDb, RenderCreateProcedureScripts(procedureMode.ProcedureName, containers))
					.Add(SeedStage.Insert,
						RenderExecuteProcedureScript(CommonScriptNames.ExecuteInsertSp, procedureMode.ProcedureName))
					.Add(SeedStage.CleanupDb,
						RenderDropProcedureScript(CommonScriptNames.DropInsertSp, procedureMode.ProcedureName)),
				
				_ => throw new NotSupportedException($"Unknown cleanup mode '{definition.GetType().Name}'")
			};
		}

		private static IReadOnlyCollection<OrderedItem<SqlScriptAction>> RenderCreateProcedureScripts(
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