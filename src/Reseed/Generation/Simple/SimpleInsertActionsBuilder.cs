using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Configuration.Simple;
using Reseed.Generation.Insertion;
using Reseed.Generation.Schema;
using Reseed.Ordering;
using Reseed.Schema;
using static Reseed.Generation.ScriptRenderer;

namespace Reseed.Generation.Simple
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
				SimpleInsertScriptDefinition => builder.Add(
					SeedStage.Insert, 
					InsertScriptRenderer.Render(containers)),
				
				SimpleInsertProcedureDefinition procedureMode => builder
					.Add(SeedStage.PrepareDb, 
						RenderCreateProcedureScripts(procedureMode.ProcedureName, containers))
					.Add(SeedStage.Insert,
						RenderExecuteProcedureScript(ScriptNames.ExecuteInsertSp, procedureMode.ProcedureName))
					.Add(SeedStage.CleanupDb,
						RenderDropProcedureScript(ScriptNames.DropInsertSp, procedureMode.ProcedureName)),
				
				_ => throw new NotSupportedException($"Unknown {nameof(SimpleInsertDefinition)} '{definition.GetType().Name}'")
			};
		}

		private static IReadOnlyCollection<OrderedItem<SqlScriptAction>> RenderCreateProcedureScripts(
			[NotNull] ObjectName name,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers)
		{
			var dropProcedure = RenderDropProcedureScript(ScriptNames.DropInsertSp, name);
			var createProcedure = InsertScriptRenderer.Render(containers)
				.Map(t => RenderCreateStoredProcedure(name, t), ScriptNames.CreateInsertSp);

			return OrderedItem.OrderedCollection(dropProcedure, createProcedure);
		}
	}
}