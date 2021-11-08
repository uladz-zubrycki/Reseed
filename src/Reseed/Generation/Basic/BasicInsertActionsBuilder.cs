using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Configuration.Basic;
using Reseed.Generation.Insertion;
using Reseed.Generation.Schema;
using Reseed.Ordering;
using Reseed.Schema;
using static Reseed.Generation.ScriptRenderer;

namespace Reseed.Generation.Basic
{
	internal static class BasicInsertActionsBuilder
	{
		public static SeedActionsBuilder AddBasicInsertActions(
			[NotNull] this SeedActionsBuilder builder,
			[NotNull] BasicInsertDefinition definition,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers)
		{
			if (builder == null) throw new ArgumentNullException(nameof(builder));
			if (containers == null) throw new ArgumentNullException(nameof(containers));
			if (definition == null) throw new ArgumentNullException(nameof(definition));

			return definition switch
			{
				BasicInsertScriptDefinition => builder.Add(
					SeedStage.Insert, 
					InsertScriptRenderer.Render(containers)),
				
				BasicInsertProcedureDefinition procedureDefinition => builder
					.Add(SeedStage.PrepareDb, 
						RenderCreateProcedureScripts(procedureDefinition.ProcedureName, containers))
					.Add(SeedStage.Insert,
						RenderExecuteProcedureScript(ScriptNames.ExecuteInsertSp, procedureDefinition.ProcedureName))
					.Add(SeedStage.CleanupDb,
						RenderDropProcedureScript(ScriptNames.DropInsertSp, procedureDefinition.ProcedureName)),
				
				_ => throw new NotSupportedException($"Unknown {nameof(BasicInsertDefinition)} '{definition.GetType().Name}'")
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