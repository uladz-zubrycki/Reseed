using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Schema;
using static Reseed.Ordering.OrderedItem;
using static Reseed.Rendering.Internals.ScriptRendererUtils;
using static Reseed.Rendering.Internals.CommonScriptNames;

namespace Reseed.Rendering.Internals
{
	internal static class StoredProcedureModeRenderer
	{
		public static IReadOnlyCollection<OrderedItem<DbScript>> RenderInsertDataProcedure(
			[NotNull] ObjectName name,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (containers == null) throw new ArgumentNullException(nameof(containers));
			if (containers.Count == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(containers));

			DbScript dropProcedure = RenderDropProcedureScript(DropInsertSp, name);
			DbScript createProcedure = ScriptModeRenderer.RenderInsertData(containers)
				.Map(t => RenderCreateStoredProcedure(name, t), CreateInsertSp);

			return OrderedCollection(dropProcedure, createProcedure);
		}

		public static IReadOnlyCollection<OrderedItem<DbScript>> RenderDeleteDataProcedure(
			[NotNull] ObjectName name,
			[NotNull] OrderedGraph<TableSchema> schemas,
			[NotNull] DataCleanupOptions options)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (schemas == null) throw new ArgumentNullException(nameof(schemas));
			if (options == null) throw new ArgumentNullException(nameof(options));

			DbScript dropProcedure = RenderDropProcedureScript(DropDeleteSp, name);
			DbScript createProcedure = DbScript
				.Join(DeleteScript, DeleteScriptRenderer.RenderDeleteScripts(schemas, options).Order())
				.Map(s => RenderCreateStoredProcedure(name, s), CreateDeleteSp);

			return OrderedCollection(dropProcedure, createProcedure);
		}
	}
}