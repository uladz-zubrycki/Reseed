using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Rendering.Internals.Decorators;
using Reseed.Schema;
using Reseed.Schema.Internals;
using Reseed.Utils;
using static Reseed.Rendering.Internals.ScriptRendererUtils;

namespace Reseed.Rendering.Internals
{
	internal static class DeleteScriptRenderer
	{
		public static IReadOnlyCollection<OrderedItem<DbScript>> RenderDeleteScripts(
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] DataCleanupOptions options)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (options == null) throw new ArgumentNullException(nameof(options));

			OrderedGraph<TableSchema> reversedTables = tables.Reverse();
			return options.Mode switch
			{
				DeleteDataCleanupMode deleteOptions =>
					RenderDeleteMode(reversedTables, options, deleteOptions),
				PreferTruncateDataCleanupMode preferTruncateMode =>
					RenderPreferTruncateMode(reversedTables, options, preferTruncateMode),
				TruncateDataCleanupMode truncateMode =>
					RenderTruncateMode(reversedTables, options, truncateMode),
				_ => throw new NotSupportedException(
					$"Unknown {nameof(DataCleanupMode)} value '{options.Mode}'")
			};
		}

		private static IReadOnlyCollection<OrderedItem<DbScript>> RenderDeleteMode(
			OrderedGraph<TableSchema> orderedTables,
			DataCleanupOptions cleanupOptions,
			DeleteDataCleanupMode deleteOptions)
		{
			IReadOnlyCollection<OrderedItem<TableSchema>> tables = orderedTables.Nodes;
			(OrderedItem<TableSchema>[] toClean, OrderedItem<TableSchema>[] rest) =
				tables.PartitionBy(o => cleanupOptions.ShouldClean(o.Value.Name));

			(OrderedItem<TableSchema>[] defaultClean, OrderedItem<TableSchema>[] customClean) =
				toClean.PartitionBy(o => !cleanupOptions.GetCustomScript(o.Value.Name, out _));

			OrderedItem<TableSchema>[] persistentTables = rest.Concat(customClean).ToArray();
			var scripts = new List<DbScript>(2)
			{
				new DbScript(
					"Delete from tables",
					string.Join(Environment.NewLine + Environment.NewLine,
						RenderDeleteFromTables(
							FilterGraph(orderedTables, defaultClean),
							ChooseIncomingRelationsGetter(
								tables,
								persistentTables,
								deleteOptions.DeleteResolutionKind))))
			};

			if (customClean.Any())
			{
				scripts.Add(new DbScript("Custom cleanup scripts", RenderCustomCleanupScripts(
					customClean,
					BuildCustomScriptGetter(cleanupOptions),
					BuildIncomingRelationsGetter(persistentTables))));
			}

			return scripts.WithNaturalOrder().ToArray();
		}

		private static IReadOnlyCollection<OrderedItem<DbScript>> RenderPreferTruncateMode(
			OrderedGraph<TableSchema> orderedTables,
			DataCleanupOptions cleanupOptions,
			PreferTruncateDataCleanupMode truncateOptions)
		{
			IReadOnlyCollection<OrderedItem<TableSchema>> tables = orderedTables.Nodes;
			Func<TableSchema, Relation<TableSchema>[]> getAllIncomingRelations = BuildIncomingRelationsGetter(tables);

			(OrderedItem<TableSchema>[] toClean, OrderedItem<TableSchema>[] rest) =
				tables.PartitionBy(o => cleanupOptions.ShouldClean(o.Value.Name));

			(OrderedItem<TableSchema>[] defaultClean, OrderedItem<TableSchema>[] customClean) =
				toClean.PartitionBy(o => !cleanupOptions.GetCustomScript(o.Value.Name, out _));

			(OrderedItem<TableSchema>[] toDelete, OrderedItem<TableSchema>[] toTruncate) =
				defaultClean.PartitionBy(o =>
					truncateOptions.ShouldUseDelete(o.Value.Name) ||
					getAllIncomingRelations(o.Value).Any());

			OrderedItem<TableSchema>[] persistentTables = rest.Concat(customClean).ToArray();

			var scripts = new List<DbScript>(3)
			{
				new DbScript(
					"Truncate tables",
					RenderTruncateTables(toTruncate.Select(o => o.Value))),
				new DbScript(
					"Delete from tables",
					RenderDeleteFromTables(
						FilterGraph(orderedTables, toDelete),
						ChooseIncomingRelationsGetter(
							tables,
							persistentTables,
							truncateOptions.DeleteResolutionKind)))
			};

			if (customClean.Any())
			{
				scripts.Add(new DbScript("Custom cleanup scripts", RenderCustomCleanupScripts(
					customClean,
					BuildCustomScriptGetter(cleanupOptions),
					BuildIncomingRelationsGetter(persistentTables))));
			}

			return scripts.WithNaturalOrder().ToArray();
		}

		private static IReadOnlyCollection<OrderedItem<DbScript>> RenderTruncateMode(
			OrderedGraph<TableSchema> orderedTables,
			DataCleanupOptions cleanupOptions,
			TruncateDataCleanupMode truncateOptions)
		{
			IReadOnlyCollection<OrderedItem<TableSchema>> tables = orderedTables.Nodes;
			Func<TableSchema, Relation<TableSchema>[]> getAllIncomingRelations = BuildIncomingRelationsGetter(tables);

			(OrderedItem<TableSchema>[] toClean, OrderedItem<TableSchema>[] rest) =
				tables.PartitionBy(o => cleanupOptions.ShouldClean(o.Value.Name));

			(OrderedItem<TableSchema>[] defaultClean, OrderedItem<TableSchema>[] customClean) =
				toClean.PartitionBy(o => !cleanupOptions.GetCustomScript(o.Value.Name, out _));

			(OrderedItem<TableSchema>[] toDelete, OrderedItem<TableSchema>[] toTruncate) =
				defaultClean.PartitionBy(o => truncateOptions.ShouldUseDelete(o.Value.Name));

			TableSchema[] tablesToTruncate = toTruncate.Select(o => o.Value).ToArray();
			Relation<TableSchema>[] foreignKeys =
				tablesToTruncate.SelectMany(getAllIncomingRelations).Distinct().ToArray();

			OrderedItem<TableSchema>[] persistentTables = rest.Concat(customClean).ToArray();

			var scripts = new List<DbScript>(5)
			{
				new DbScript("Drop Foreign Keys", RenderDropForeignKeys(foreignKeys, false)),
				new DbScript("Truncate from tables", RenderTruncateTables(tablesToTruncate)),
				new DbScript("Delete from tables", RenderDeleteFromTables(
					FilterGraph(orderedTables, toDelete),
					ChooseIncomingRelationsGetter(
						tables,
						persistentTables,
						truncateOptions.DeleteResolutionKind)))
			};

			if (customClean.Any())
			{
				scripts.Add(new DbScript("Custom cleanup scripts", RenderCustomCleanupScripts(
					customClean,
					BuildCustomScriptGetter(cleanupOptions),
					BuildIncomingRelationsGetter(persistentTables))));
			}

			scripts.Add(new DbScript("Create Foreign Keys", RenderCreateForeignKeys(foreignKeys)));
			return scripts.WithNaturalOrder().ToArray();
		}

		private static Func<TableSchema, Relation<TableSchema>[]> ChooseIncomingRelationsGetter(
			IEnumerable<OrderedItem<TableSchema>> allTables,
			IEnumerable<OrderedItem<TableSchema>> persistentTables,
			DeleteConstraintsResolutionKind resolutionKind) =>
			resolutionKind switch
			{
				DeleteConstraintsResolutionKind.OrderTables => BuildIncomingRelationsGetter(persistentTables),
				DeleteConstraintsResolutionKind.DisableConstraints => BuildIncomingRelationsGetter(allTables),
				_ => throw new NotSupportedException(
					$"Unknown {nameof(DeleteConstraintsResolutionKind)} value '{resolutionKind}'")
			};

		private static string RenderTruncateTables(IEnumerable<TableSchema> tables) =>
			string.Join(Environment.NewLine,
				tables.Select(s => $"TRUNCATE TABLE {s.Name.GetSqlName()};"));

		private static string RenderDeleteFromTables(
			OrderedGraph<TableSchema> tables,
			Func<TableSchema, Relation<TableSchema>[]> getIncomingRelations)
		{
			return string.Join(Environment.NewLine,
					MutualReferenceResolver.MergeChunks(
							tables,
							ts => string.Join(
								Environment.NewLine,
								ts.Select(t => RenderCleanupTables(
									new[] { t.Value },
									getIncomingRelations(t.Value),
									GetCleanupScript))),
							ms =>
							{
								Relation<TableSchema>[] foreignKeys = ms.Relations
									.Concat(ms.Items.SelectMany(o => getIncomingRelations(o.Value)))
									.Distinct()
									.ToArray();

								return RenderCleanupTables(
									ms.Items.Order(),
									foreignKeys,
									GetCleanupScript);
							},
							MutualGroupOrderMode.Min)
						.Order())
				.MergeEmptyLines();

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
			string decoratedSeparator = foreignKeys.Any() ? Environment.NewLine : string.Empty;
			var fkDecorator = new DisableForeignKeysDecorator(
				foreignKeys
					.Select(r => r.Map(t => t.Name))
					.ToArray());

			return string.Join(string.Empty,
				decoratedSeparator,
				fkDecorator.Decorate(string.Join(Environment.NewLine,
					tables
						.Select(s => getCleanupScript(s.Name)))),
				decoratedSeparator);
		}

		private static OrderedGraph<TableSchema> FilterGraph(
			OrderedGraph<TableSchema> allTables,
			IReadOnlyCollection<OrderedItem<TableSchema>> targetTables)
		{
			HashSet<TableSchema> targetTablesSet = targetTables
				.Select(t => t.Value)
				.ToHashSet();

			return allTables.FilterShallow(targetTablesSet.Contains);
		}

		private static Func<TableSchema, Relation<TableSchema>[]> BuildIncomingRelationsGetter(
			IEnumerable<OrderedItem<TableSchema>> tables)
		{
			Dictionary<TableSchema, Relation<TableSchema>[]> incomingRelationMap =
				tables
					.SelectMany(ot => ot.Value.GetRelations())
					.GroupBy(t => t.Target)
					.ToDictionary(gr => gr.Key,
						gr => gr.ToArray());

			return t =>
				incomingRelationMap.TryGetValue(t, out Relation<TableSchema>[] rs)
					? rs
					: Array.Empty<Relation<TableSchema>>();
		}

		private static Func<ObjectName, string> BuildCustomScriptGetter(DataCleanupOptions options) =>
			t => options.GetCustomScript(t, out string s)
				? s
				: throw new InvalidOperationException($"There is no custom script for table {t}");
	}
}