using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Configuration.Cleanup;
using Reseed.Generation.Insertion;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Schema;
using Reseed.Utils;
using static Reseed.Generation.ScriptRenderer;

namespace Reseed.Generation.Cleanup
{
	internal static class DeleteScriptRenderer
	{
		public static IReadOnlyCollection<OrderedItem<SqlScriptAction>> Render(
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] CleanupConfiguration configuration)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (configuration == null) throw new ArgumentNullException(nameof(configuration));

			var reversedTables = tables.Reverse();
			var cleanupScripts = configuration.Mode switch
			{
				DeleteCleanupMode deleteMode =>
					RenderDeleteScripts(reversedTables, configuration.Target, deleteMode),
				PreferTruncateCleanupMode preferTruncateMode =>
					RenderPreferTruncateScripts(reversedTables, configuration.Target, preferTruncateMode),
				TruncateCleanupMode truncateMode =>
					RenderTruncateScripts(reversedTables, configuration.Target, truncateMode),
				_ => throw new NotSupportedException(
					$"Unknown {nameof(CleanupMode)} value '{configuration.Mode}'")
			};

			return configuration.ReseedIdentityColumns
				? AppendReseedScript(cleanupScripts, reversedTables)
				: cleanupScripts;
		}

		private static IReadOnlyCollection<OrderedItem<SqlScriptAction>> RenderDeleteScripts(
			OrderedGraph<TableSchema> orderedTables,
			CleanupTarget cleanupTarget,
			DeleteCleanupMode cleanupMode)
		{
			var tables = orderedTables.Nodes;
			var (toClean, rest) =
				tables.PartitionBy(o => cleanupTarget.ShouldClean(o.Value.Name));

			var (defaultClean, customClean) =
				toClean.PartitionBy(o => !cleanupTarget.GetCustomScript(o.Value.Name, out _));

			var persistentTables = rest.Concat(customClean).ToArray();
			var getDefaultIncomingRelations = ChooseIncomingRelationsGetter(
				tables,
				persistentTables,
				cleanupMode.ConstraintBehavior);
			var getCustomIncomingRelations = BuildIncomingRelationsGetter(persistentTables);
			var shouldDropConstraints =
				cleanupMode.ConstraintBehavior == ConstraintResolutionBehavior.DropConstraints;
			var foreignKeys = shouldDropConstraints
				? defaultClean
					.SelectMany(o => getDefaultIncomingRelations(o.Value))
					.Concat(customClean.SelectMany(o => getCustomIncomingRelations(o.Value)))
					.ToArray()
				: Array.Empty<Relation<TableSchema>>();

			var cleanupScripts = new List<SqlScriptAction>(4)
				.AddScriptWhen(
					() => new SqlScriptAction("Drop Foreign Keys",
						RenderDropForeignKeys(foreignKeys, false)),
					foreignKeys.Length > 0)
				.AddScriptWhen(
					() => new SqlScriptAction("Delete from tables",
						string.Join(Environment.NewLine + Environment.NewLine,
							RenderDeleteFromTables(
								FilterGraph(orderedTables, defaultClean),
								shouldDropConstraints
									? GetNoIncomingRelations
									: getDefaultIncomingRelations,
								cleanupMode.ConstraintBehavior))),
					defaultClean.Length > 0)
				.AddScriptWhen(() => new SqlScriptAction("Custom cleanup scripts",
						RenderCustomCleanupScripts(
							customClean,
							BuildCustomScriptGetter(cleanupTarget),
							shouldDropConstraints
								? GetNoIncomingRelations
								: getCustomIncomingRelations)),
					customClean.Length > 0)
				.AddScriptWhen(
					() => new SqlScriptAction("Create Foreign Keys",
						RenderCreateForeignKeys(foreignKeys)),
					foreignKeys.Length > 0)
				.WithNaturalOrder()
				.ToArray();

			return WrapDroppedConstraintCleanupInTransaction(
				cleanupScripts,
				shouldDropConstraints && foreignKeys.Length > 0);
		}

		private static IReadOnlyCollection<OrderedItem<SqlScriptAction>> RenderPreferTruncateScripts(
			OrderedGraph<TableSchema> orderedTables,
			CleanupTarget cleanupTarget,
			PreferTruncateCleanupMode cleanupMode)
		{
			var tables = orderedTables.Nodes;
			var getAllIncomingRelations = BuildIncomingRelationsGetter(tables);

			var (toClean, rest) =
				tables.PartitionBy(o => cleanupTarget.ShouldClean(o.Value.Name));

			var (defaultClean, customClean) =
				toClean.PartitionBy(o => !cleanupTarget.GetCustomScript(o.Value.Name, out _));

			var shouldDropConstraints =
				cleanupMode.ConstraintBehavior == ConstraintResolutionBehavior.DropConstraints;
			var (toDelete, toTruncate) =
				defaultClean.PartitionBy(o =>
					cleanupMode.ShouldUseDelete(o.Value.Name) ||
					o.Value.IsReferencedByIndexedView ||
					(!shouldDropConstraints && getAllIncomingRelations(o.Value).Any()));

			var persistentTables = rest.Concat(customClean).ToArray();
			var getDeleteIncomingRelations = ChooseIncomingRelationsGetter(
				tables,
				persistentTables,
				cleanupMode.ConstraintBehavior);
			var getCustomIncomingRelations = BuildIncomingRelationsGetter(persistentTables);
			var foreignKeys = shouldDropConstraints
				? toTruncate
					.Unordered()
					.SelectMany(getAllIncomingRelations)
					.Concat(toDelete.SelectMany(o => getDeleteIncomingRelations(o.Value)))
					.Concat(customClean.SelectMany(o => getCustomIncomingRelations(o.Value)))
					.ToArray()
				: Array.Empty<Relation<TableSchema>>();

			var cleanupScripts = new List<SqlScriptAction>(5)
				.AddScriptWhen(
					() => new SqlScriptAction("Drop Foreign Keys",
						RenderDropForeignKeys(foreignKeys, false)),
					foreignKeys.Length > 0)
				.AddScriptWhen(
					() => new SqlScriptAction("Truncate tables",
						RenderTruncateTables(toTruncate.Unordered())),
					toTruncate.Length > 0)
				.AddScriptWhen(
					() => new SqlScriptAction("Delete from tables",
						RenderDeleteFromTables(
							FilterGraph(orderedTables, toDelete),
							shouldDropConstraints
								? GetNoIncomingRelations
								: getDeleteIncomingRelations,
							cleanupMode.ConstraintBehavior)),
					toDelete.Length > 0)
				.AddScriptWhen(
					() => new SqlScriptAction("Custom cleanup scripts",
						RenderCustomCleanupScripts(
							customClean,
							BuildCustomScriptGetter(cleanupTarget),
							shouldDropConstraints
								? GetNoIncomingRelations
								: getCustomIncomingRelations)),
					customClean.Length > 0)
				.AddScriptWhen(
					() => new SqlScriptAction("Create Foreign Keys",
						RenderCreateForeignKeys(foreignKeys)),
					foreignKeys.Length > 0)
				.WithNaturalOrder()
				.ToArray();

			return WrapDroppedConstraintCleanupInTransaction(
				cleanupScripts,
				shouldDropConstraints && foreignKeys.Length > 0);
		}

		private static IReadOnlyCollection<OrderedItem<SqlScriptAction>> RenderTruncateScripts(
			OrderedGraph<TableSchema> orderedTables,
			CleanupTarget cleanupTarget,
			TruncateCleanupMode cleanupMode)
		{
			var tables = orderedTables.Nodes;
			var getAllIncomingRelations = BuildIncomingRelationsGetter(tables);

			var (toClean, rest) =
				tables.PartitionBy(o => cleanupTarget.ShouldClean(o.Value.Name));

			var (defaultClean, customClean) =
				toClean.PartitionBy(o => !cleanupTarget.GetCustomScript(o.Value.Name, out _));

			var (toDelete, toTruncate) =
				defaultClean.PartitionBy(o => cleanupMode.ShouldUseDelete(o.Value.Name));

			var tablesToTruncate = toTruncate.Unordered().ToArray();
			var tablesReferencedByIndexedViews = tablesToTruncate
				.Where(t => t.IsReferencedByIndexedView)
				.Select(t => t.Name)
				.ToArray();
			if (tablesReferencedByIndexedViews.Length > 0)
			{
				throw new InvalidOperationException(
					$"Cleanup mode '{nameof(CleanupMode.Truncate)}' can't truncate tables referenced by indexed views: " +
					$"{string.Join(", ", tablesReferencedByIndexedViews.Select(t => t.ToString()))}. " +
					"Add these tables to the useDeleteForTables argument or use CleanupMode.PreferTruncate().");
			}

			var persistentTables = rest.Concat(customClean).ToArray();
			var getDeleteIncomingRelations = ChooseIncomingRelationsGetter(
				tables,
				persistentTables,
				cleanupMode.ConstraintBehavior);
			var getCustomIncomingRelations = BuildIncomingRelationsGetter(persistentTables);
			var shouldDropConstraints =
				cleanupMode.ConstraintBehavior == ConstraintResolutionBehavior.DropConstraints;
			var foreignKeys = tablesToTruncate
				.SelectMany(getAllIncomingRelations)
				.Concat(shouldDropConstraints
					? toDelete
						.SelectMany(o => getDeleteIncomingRelations(o.Value))
						.Concat(customClean.SelectMany(o => getCustomIncomingRelations(o.Value)))
					: Enumerable.Empty<Relation<TableSchema>>())
				.ToArray();

			var cleanupScripts = new List<SqlScriptAction>(5)
				.AddScriptWhen(
					() => new SqlScriptAction("Drop Foreign Keys",
						RenderDropForeignKeys(foreignKeys, false)),
					foreignKeys.Length > 0)
				.AddScriptWhen(
					() => new SqlScriptAction("Truncate from tables",
						RenderTruncateTables(tablesToTruncate)),
					tablesToTruncate.Length > 0)
				.AddScriptWhen(
					() => new SqlScriptAction("Delete from tables",
						RenderDeleteFromTables(
							FilterGraph(orderedTables, toDelete),
							shouldDropConstraints
								? GetNoIncomingRelations
								: getDeleteIncomingRelations,
							cleanupMode.ConstraintBehavior)),
					toDelete.Length > 0)
				.AddScriptWhen(
					() => new SqlScriptAction("Custom cleanup scripts",
						RenderCustomCleanupScripts(
							customClean,
							BuildCustomScriptGetter(cleanupTarget),
							shouldDropConstraints
								? GetNoIncomingRelations
								: getCustomIncomingRelations)),
					customClean.Length > 0)
				.AddScriptWhen(
					() => new SqlScriptAction("Create Foreign Keys",
						RenderCreateForeignKeys(foreignKeys)),
					foreignKeys.Length > 0)
				.WithNaturalOrder()
				.ToArray();

			return WrapDroppedConstraintCleanupInTransaction(
				cleanupScripts,
				shouldDropConstraints && foreignKeys.Length > 0);
		}

		private static Func<TableSchema, Relation<TableSchema>[]> ChooseIncomingRelationsGetter(
			IEnumerable<OrderedItem<TableSchema>> allTables,
			IEnumerable<OrderedItem<TableSchema>> persistentTables,
			ConstraintResolutionBehavior resolutionKind) =>
			resolutionKind switch
			{
				ConstraintResolutionBehavior.OrderTables => BuildIncomingRelationsGetter(persistentTables),
				ConstraintResolutionBehavior.DisableConstraints => BuildIncomingRelationsGetter(allTables),
				ConstraintResolutionBehavior.DropConstraints => BuildIncomingRelationsGetter(allTables),
				_ => throw new NotSupportedException(
					$"Unknown {nameof(ConstraintResolutionBehavior)} value '{resolutionKind}'")
			};

		private static string RenderTruncateTables(IEnumerable<TableSchema> tables) =>
			string.Join(Environment.NewLine,
				tables.Select(s => $"TRUNCATE TABLE {s.Name.GetSqlName()};"));

		private static string RenderDeleteFromTables(
			OrderedGraph<TableSchema> tables,
			Func<TableSchema, Relation<TableSchema>[]> getIncomingRelations,
			ConstraintResolutionBehavior constraintBehavior)
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
					var foreignKeys =
						constraintBehavior == ConstraintResolutionBehavior.DropConstraints
							? Array.Empty<Relation<TableSchema>>()
							: ms.Relations
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

		private static IReadOnlyCollection<OrderedItem<SqlScriptAction>>
			WrapDroppedConstraintCleanupInTransaction(
				IReadOnlyCollection<OrderedItem<SqlScriptAction>> scripts,
				bool shouldWrap)
		{
			if (!shouldWrap)
			{
				return scripts;
			}

			var cleanupScript = SqlScriptAction.Join(
				"Cleanup with dropped constraints",
				scripts);

			return OrderedItem.OrderedCollection(
				cleanupScript.Map(
					script => $@"
						|DECLARE @ReseedXactAbortWasOn bit =
						|	CASE WHEN (16384 & @@OPTIONS) = 16384 THEN 1 ELSE 0 END;
						|SET XACT_ABORT ON;
						|BEGIN TRY
						|	BEGIN TRANSACTION;
						|
						{script.WithMargin("\t", '|')}
						|
						|	COMMIT TRANSACTION;
						|	IF @ReseedXactAbortWasOn = 0
						|		SET XACT_ABORT OFF;
						|END TRY
						|BEGIN CATCH
						|	IF @@TRANCOUNT > 0
						|		ROLLBACK TRANSACTION;
						|	IF @ReseedXactAbortWasOn = 0
						|		SET XACT_ABORT OFF;
						|	THROW;
						|END CATCH"
						.TrimMargin('|')));
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
					.ToDictionary(gr => gr.Key, gr => gr.ToArray());

			return t =>
				incomingRelationMap.TryGetValue(t, out var rs)
					? rs
					: Array.Empty<Relation<TableSchema>>();
		}

		private static Relation<TableSchema>[] GetNoIncomingRelations(TableSchema _) =>
			Array.Empty<Relation<TableSchema>>();

		private static Func<ObjectName, string> BuildCustomScriptGetter(CleanupTarget target) =>
			t => target.GetCustomScript(t, out var s)
				? s
				: throw new InvalidOperationException($"There is no custom script for table {t}");

		private static IReadOnlyCollection<OrderedItem<SqlScriptAction>> AppendReseedScript(
			IReadOnlyCollection<OrderedItem<SqlScriptAction>> scripts, 
			OrderedGraph<TableSchema> orderedTables)
		{
			var identityTables = orderedTables.Nodes
				.Unordered()
				.Where(t => t.Columns.Any(c => c.IsIdentity))
				.ToArray();

			if (identityTables.Length == 0)
			{
				return scripts;
			}
			else
			{
				return scripts
					.Append(IdentityReseedScriptRenderer.Render(identityTables))
					.ToArray();
			}
		}
	}
}