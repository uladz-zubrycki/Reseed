using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Configuration.Cleanup;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Rendering.Insertion;
using Reseed.Schema;
using Reseed.Utils;
using static Reseed.Rendering.Scripts;

namespace Reseed.Rendering.Cleanup
{
	internal static class DeleteScriptRenderer
	{
		public static IReadOnlyCollection<OrderedItem<SqlScriptAction>> Render(
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] CleanupOptions options)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (options == null) throw new ArgumentNullException(nameof(options));

			var reversedTables = tables.Reverse();
			return options.Kind switch
			{
				DeleteCleanupKind deleteOptions =>
					RenderDeleteKind(reversedTables, options, deleteOptions),
				PreferTruncateCleanupKind preferTruncateMode =>
					RenderPreferTruncateKind(reversedTables, options, preferTruncateMode),
				TruncateCleanupKind truncateMode =>
					RenderTruncateKind(reversedTables, options, truncateMode),
				_ => throw new NotSupportedException(
					$"Unknown {nameof(CleanupKind)} value '{options.Kind}'")
			};
		}

		private static IReadOnlyCollection<OrderedItem<SqlScriptAction>> RenderDeleteKind(
			OrderedGraph<TableSchema> orderedTables,
			CleanupOptions cleanupOptions,
			DeleteCleanupKind deleteKind)
		{
			var tables = orderedTables.Nodes;
			var (toClean, rest) =
				tables.PartitionBy(o => cleanupOptions.ShouldClean(o.Value.Name));

			var (defaultClean, customClean) =
				toClean.PartitionBy(o => !cleanupOptions.GetCustomScript(o.Value.Name, out _));

			var persistentTables = rest.Concat(customClean).ToArray();
			return new List<SqlScriptAction>(2)
				.AddScriptWhen(
					() => new SqlScriptAction("Delete from tables",
						string.Join(Environment.NewLine + Environment.NewLine,
							RenderDeleteFromTables(
								FilterGraph(orderedTables, defaultClean),
								ChooseIncomingRelationsGetter(
									tables,
									persistentTables,
									deleteKind.ConstraintsResolution)))),
					defaultClean.Length > 0)
				.AddScriptWhen(() => new SqlScriptAction("Custom cleanup scripts", RenderCustomCleanupScripts(
						customClean,
						BuildCustomScriptGetter(cleanupOptions),
						BuildIncomingRelationsGetter(persistentTables))),
					customClean.Length > 0)
				.WithNaturalOrder()
				.ToArray();
		}

		private static IReadOnlyCollection<OrderedItem<SqlScriptAction>> RenderPreferTruncateKind(
			OrderedGraph<TableSchema> orderedTables,
			CleanupOptions cleanupOptions,
			PreferTruncateCleanupKind truncateKind)
		{
			var tables = orderedTables.Nodes;
			var getAllIncomingRelations = BuildIncomingRelationsGetter(tables);

			var (toClean, rest) =
				tables.PartitionBy(o => cleanupOptions.ShouldClean(o.Value.Name));

			var (defaultClean, customClean) =
				toClean.PartitionBy(o => !cleanupOptions.GetCustomScript(o.Value.Name, out _));

			var (toDelete, toTruncate) =
				defaultClean.PartitionBy(o =>
					truncateKind.ShouldUseDelete(o.Value.Name) ||
					getAllIncomingRelations(o.Value).Any());

			var persistentTables = rest.Concat(customClean).ToArray();

			return new List<SqlScriptAction>(3)
				.AddScriptWhen(
					() => new SqlScriptAction("Truncate tables", RenderTruncateTables(toTruncate.Unordered())),
					toTruncate.Length > 0)
				.AddScriptWhen(
					() => new SqlScriptAction("Delete from tables",
						RenderDeleteFromTables(
							FilterGraph(orderedTables, toDelete),
							ChooseIncomingRelationsGetter(
								tables,
								persistentTables,
								truncateKind.ConstraintsResolution))),
					toDelete.Length > 0)
				.AddScriptWhen(
					() => new SqlScriptAction("Custom cleanup scripts", RenderCustomCleanupScripts(
						customClean,
						BuildCustomScriptGetter(cleanupOptions),
						BuildIncomingRelationsGetter(persistentTables))),
					customClean.Length > 0)
				.WithNaturalOrder()
				.ToArray();
		}

		private static IReadOnlyCollection<OrderedItem<SqlScriptAction>> RenderTruncateKind(
			OrderedGraph<TableSchema> orderedTables,
			CleanupOptions cleanupOptions,
			TruncateCleanupKind truncateKind)
		{
			var tables = orderedTables.Nodes;
			var getAllIncomingRelations = BuildIncomingRelationsGetter(tables);

			var (toClean, rest) =
				tables.PartitionBy(o => cleanupOptions.ShouldClean(o.Value.Name));

			var (defaultClean, customClean) =
				toClean.PartitionBy(o => !cleanupOptions.GetCustomScript(o.Value.Name, out _));

			var (toDelete, toTruncate) =
				defaultClean.PartitionBy(o => truncateKind.ShouldUseDelete(o.Value.Name));

			var tablesToTruncate = toTruncate.Unordered().ToArray();
			var foreignKeys =
				tablesToTruncate.SelectMany(getAllIncomingRelations).Distinct().ToArray();

			var persistentTables = rest.Concat(customClean).ToArray();

			return new List<SqlScriptAction>(5)
				.AddScriptWhen(
					() => new SqlScriptAction("Drop Foreign Keys", RenderDropForeignKeys(foreignKeys, false)),
					foreignKeys.Length > 0)
				.AddScriptWhen(
					() => new SqlScriptAction("Truncate from tables", RenderTruncateTables(tablesToTruncate)),
					tablesToTruncate.Length > 0)
				.AddScriptWhen(
					() => new SqlScriptAction("Delete from tables", RenderDeleteFromTables(
						FilterGraph(orderedTables, toDelete),
						ChooseIncomingRelationsGetter(
							tables,
							persistentTables,
							truncateKind.ConstraintsResolution))),
					toDelete.Length > 0)
				.AddScriptWhen(
					() => new SqlScriptAction("Custom cleanup scripts", RenderCustomCleanupScripts(
						customClean,
						BuildCustomScriptGetter(cleanupOptions),
						BuildIncomingRelationsGetter(persistentTables))),
					customClean.Length > 0)
				.AddScriptWhen(
					() => new SqlScriptAction("Create Foreign Keys", RenderCreateForeignKeys(foreignKeys)),
					foreignKeys.Length > 0)
				.WithNaturalOrder()
				.ToArray();
		}

		private static Func<TableSchema, Relation<TableSchema>[]> ChooseIncomingRelationsGetter(
			IEnumerable<OrderedItem<TableSchema>> allTables,
			IEnumerable<OrderedItem<TableSchema>> persistentTables,
			ConstraintsResolutionKind resolutionKind) =>
			resolutionKind switch
			{
				ConstraintsResolutionKind.OrderTables => BuildIncomingRelationsGetter(persistentTables),
				ConstraintsResolutionKind.DisableConstraints => BuildIncomingRelationsGetter(allTables),
				_ => throw new NotSupportedException(
					$"Unknown {nameof(ConstraintsResolutionKind)} value '{resolutionKind}'")
			};

		private static string RenderTruncateTables(IEnumerable<TableSchema> tables) =>
			string.Join(Environment.NewLine,
				tables.Select(s => $"TRUNCATE TABLE {s.Name.GetSqlName()};"));

		private static string RenderDeleteFromTables(
			OrderedGraph<TableSchema> tables,
			Func<TableSchema, Relation<TableSchema>[]> getIncomingRelations)
		{
			var scripts = MutualReferenceResolver.MergeChunks(
				tables,
				ts => string.Join(
					Environment.NewLine,
					ts.Select(t => RenderCleanupTables(
						new[] { t.Value },
						getIncomingRelations(t.Value),
						GetCleanupScript))),
				ms =>
				{
					var foreignKeys = ms.Relations
						.Concat(ms.Items.SelectMany(o => getIncomingRelations(o.Value)))
						.Distinct()
						.ToArray();

					return RenderCleanupTables(
						ms.Items.Order(),
						foreignKeys,
						GetCleanupScript);
				},
				MutualGroupOrderMode.Min);

			return string.Join(Environment.NewLine, scripts.Order()).MergeEmptyLines();

			static string GetCleanupScript(ObjectName t) => $"DELETE FROM {t.GetSqlName()};";
		}

		private static string RenderCustomCleanupScripts(
			OrderedItem<TableSchema>[] tables,
			Func<ObjectName, string> getCleanupScript,
			Func<TableSchema, Relation<TableSchema>[]> getIncomingRelations) =>
			string.Join(Environment.NewLine + Environment.NewLine,
				tables.Order().Select(t =>
					RenderCleanupTables(new[] { t },
						getIncomingRelations(t),
						getCleanupScript)));

		private static string RenderCleanupTables(
			IEnumerable<TableSchema> tables,
			IReadOnlyCollection<Relation<TableSchema>> foreignKeys,
			Func<ObjectName, string> getCleanupScript)
		{
			var decoratedSeparator = foreignKeys.Any() ? Environment.NewLine : string.Empty;
			var fkDecorator = new DisableForeignKeysDecorator(
				foreignKeys
					.Select(r => r.Map(t => t.Name))
					.ToArray());

			return string.Join(string.Empty,
				decoratedSeparator,
				fkDecorator.Decorate(string.Join(Environment.NewLine,
					tables.Select(s => getCleanupScript(s.Name)))),
				decoratedSeparator);
		}

		private static OrderedGraph<TableSchema> FilterGraph(
			OrderedGraph<TableSchema> allTables,
			IReadOnlyCollection<OrderedItem<TableSchema>> targetTables)
		{
			var targetTablesSet = targetTables
				.Select(t => t.Value)
				.ToHashSet();

			return targetTablesSet.Count > 0
				? allTables.FilterShallow(targetTablesSet.Contains)
				: OrderedGraph<TableSchema>.Empty;
		}

		private static Func<TableSchema, Relation<TableSchema>[]> BuildIncomingRelationsGetter(
			IEnumerable<OrderedItem<TableSchema>> tables)
		{
			var incomingRelationMap =
				tables
					.SelectMany(ot => ot.Value.GetRelations())
					.GroupBy(t => t.Target)
					.ToDictionary(gr => gr.Key,
						gr => gr.ToArray());

			return t =>
				incomingRelationMap.TryGetValue(t, out var rs)
					? rs
					: Array.Empty<Relation<TableSchema>>();
		}

		private static Func<ObjectName, string> BuildCustomScriptGetter(CleanupOptions options) =>
			t => options.GetCustomScript(t, out var s)
				? s
				: throw new InvalidOperationException($"There is no custom script for table {t}");
	}
}