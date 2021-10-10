using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Rendering.Decorators;
using Reseed.Schema;
using Reseed.Utils;
using static Reseed.Rendering.ScriptRendererUtils;

namespace Reseed.Rendering
{
	internal static class DeleteScriptRenderer
	{
		public static IReadOnlyCollection<OrderedItem<DbScript>> RenderDeleteScripts(
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] CleanupOptions options)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (options == null) throw new ArgumentNullException(nameof(options));

			var reversedTables = tables.Reverse();
			return options.Kind switch
			{
				DeleteCleanupKind deleteOptions =>
					RenderDeleteMode(reversedTables, options, deleteOptions),
				PreferTruncateCleanupKind preferTruncateMode =>
					RenderPreferTruncateMode(reversedTables, options, preferTruncateMode),
				TruncateCleanupKind truncateMode =>
					RenderTruncateMode(reversedTables, options, truncateMode),
				_ => throw new NotSupportedException(
					$"Unknown {nameof(CleanupKind)} value '{options.Kind}'")
			};
		}

		private static IReadOnlyCollection<OrderedItem<DbScript>> RenderDeleteMode(
			OrderedGraph<TableSchema> orderedTables,
			CleanupOptions cleanupOptions,
			DeleteCleanupKind deleteOptions)
		{
			var tables = orderedTables.Nodes;
			var (toClean, rest) =
				tables.PartitionBy(o => cleanupOptions.ShouldClean(o.Value.Name));

			var (defaultClean, customClean) =
				toClean.PartitionBy(o => !cleanupOptions.GetCustomScript(o.Value.Name, out _));

			var persistentTables = rest.Concat(customClean).ToArray();
			var scripts = new List<DbScript>(2)
			{
				new(
					"Delete from tables",
					string.Join(Environment.NewLine + Environment.NewLine,
						RenderDeleteFromTables(
							FilterGraph(orderedTables, defaultClean),
							ChooseIncomingRelationsGetter(
								tables,
								persistentTables,
								deleteOptions.ConstraintsResolution))))
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
			CleanupOptions cleanupOptions,
			PreferTruncateCleanupKind truncateOptions)
		{
			var tables = orderedTables.Nodes;
			var getAllIncomingRelations = BuildIncomingRelationsGetter(tables);

			var (toClean, rest) =
				tables.PartitionBy(o => cleanupOptions.ShouldClean(o.Value.Name));

			var (defaultClean, customClean) =
				toClean.PartitionBy(o => !cleanupOptions.GetCustomScript(o.Value.Name, out _));

			var (toDelete, toTruncate) =
				defaultClean.PartitionBy(o =>
					truncateOptions.ShouldUseDelete(o.Value.Name) ||
					getAllIncomingRelations(o.Value).Any());

			var persistentTables = rest.Concat(customClean).ToArray();

			var scripts = new List<DbScript>(3);
			if (toTruncate.Length > 0)
			{
				scripts.Add(new DbScript(
					"Truncate tables",
					RenderTruncateTables(toTruncate.Select(o => o.Value))));
			}

			if (toDelete.Length > 0)
			{
				scripts.Add(new DbScript(
					"Delete from tables",
					RenderDeleteFromTables(
						FilterGraph(orderedTables, toDelete),
						ChooseIncomingRelationsGetter(
							tables,
							persistentTables,
							truncateOptions.ConstraintsResolution))));
			}

			if (customClean.Length > 0)
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
			CleanupOptions cleanupOptions,
			TruncateCleanupKind truncateOptions)
		{
			var tables = orderedTables.Nodes;
			var getAllIncomingRelations = BuildIncomingRelationsGetter(tables);

			var (toClean, rest) =
				tables.PartitionBy(o => cleanupOptions.ShouldClean(o.Value.Name));

			var (defaultClean, customClean) =
				toClean.PartitionBy(o => !cleanupOptions.GetCustomScript(o.Value.Name, out _));

			var (toDelete, toTruncate) =
				defaultClean.PartitionBy(o => truncateOptions.ShouldUseDelete(o.Value.Name));

			var tablesToTruncate = toTruncate.Select(o => o.Value).ToArray();
			var foreignKeys =
				tablesToTruncate.SelectMany(getAllIncomingRelations).Distinct().ToArray();

			var persistentTables = rest.Concat(customClean).ToArray();

			var scripts = new List<DbScript>(5)
			{
				new("Drop Foreign Keys", RenderDropForeignKeys(foreignKeys, false)),
				new("Truncate from tables", RenderTruncateTables(tablesToTruncate)),
				new("Delete from tables", RenderDeleteFromTables(
					FilterGraph(orderedTables, toDelete),
					ChooseIncomingRelationsGetter(
						tables,
						persistentTables,
						truncateOptions.ConstraintsResolution)))
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
								var foreignKeys = ms.Relations
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
			var decoratedSeparator = foreignKeys.Any() ? Environment.NewLine : string.Empty;
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