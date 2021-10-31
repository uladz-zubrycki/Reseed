using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Configuration.TemporaryTables;
using Reseed.Generation.Cleanup;
using Reseed.Generation.Schema;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Schema;
using Reseed.Utils;
using static Reseed.Generation.ScriptRenderer;

namespace Reseed.Generation.TemporaryTables
{
	internal static class TemporaryTablesActionGenerator
	{
		public static SeedActions Generate(
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers,
			[NotNull] TemporaryTablesMode mode)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (containers == null) throw new ArgumentNullException(nameof(containers));
			if (mode == null) throw new ArgumentNullException(nameof(mode));

			return new SeedActionsBuilder()
				.AddCleanupActions(mode.CleanupDefinition, tables)
				.Add(SeedStage.PrepareDb, RenderInitScripts(
					mode.SchemaName,
					tables,
					containers))
				.AddInsertFrom(
					mode.InsertDefinition,
					mode.SchemaName,
					tables,
					containers)
				.Add(SeedStage.CleanupDb, RenderDropScripts(
					mode.SchemaName,
					tables,
					containers))
				.Build();
		}

		private static IReadOnlyCollection<OrderedItem<SqlScriptAction>> RenderInitScripts(
			[NotNull] string tempSchemaName,
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers)
		{
			return TemporaryTablesInitScriptRenderer.Render(
				tempSchemaName,
				MapTables(tempSchemaName, tables, containers),
				MapContainers(tempSchemaName, containers));
		}

		private static SeedActionsBuilder AddInsertFrom(
			[NotNull] this SeedActionsBuilder builder,
			[NotNull] TemporaryTablesInsertDefinition insertDefinition,
			[NotNull] string tempSchemaName,
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers)
		{
			var filteredGraph = FilterTables(tables, containers);
			return insertDefinition switch
			{
				TemporaryTablesInsertScriptDefinition =>
					builder.Add(SeedStage.Insert,
						TemporaryTableInsertScriptRenderer.Render(
							filteredGraph,
							n => CreateTempTableName(tempSchemaName, n))),

				TemporaryTablesInsertProcedureDefinition procedureDefinition =>
					AddInsertProcedure(
						builder,
						procedureDefinition.ProcedureName,
						filteredGraph,
						n => CreateTempTableName(tempSchemaName, n)),

				TemporaryTablesInsertSqlBulkCopyDefinition bulkCopyDefinition =>
					builder.Add(SeedStage.Insert,
						TemporaryTablesSqlBulkCopyGenerator.GenerateInsertActions(
							filteredGraph,
							n => CreateTempTableName(tempSchemaName, n),
							bulkCopyDefinition)),

				_ => throw new NotSupportedException(
					$"Unknown {nameof(TemporaryTablesInsertDefinition)} '{insertDefinition.GetType().FullName}' value")
			};
		}

		private static SeedActionsBuilder AddInsertProcedure(
			[NotNull] SeedActionsBuilder builder,
			[NotNull] ObjectName procedureName,
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] Func<ObjectName, ObjectName> mapTableName)
		{
			var createProcedure = TemporaryTableInsertScriptRenderer.Render(tables, mapTableName)
				.Map(s => RenderCreateStoredProcedure(procedureName, s), ScriptNames.CreateInsertSp);

			var dropProcedure = RenderDropProcedureScript(
				ScriptNames.DropInsertSp,
				procedureName);

			return builder
				.Add(SeedStage.PrepareDb, dropProcedure, createProcedure)
				.Add(SeedStage.Insert, RenderExecuteProcedureScript(
					ScriptNames.ExecuteInsertSp, procedureName))
				.Add(SeedStage.CleanupDb, dropProcedure);
		}

		private static IReadOnlyCollection<OrderedItem<SqlScriptAction>> RenderDropScripts(
			[NotNull] string tempSchemaName,
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers) =>
			TemporaryTablesCleanupScriptRenderer.Render(
				tempSchemaName,
				MapTables(tempSchemaName, tables, containers));

		private static ObjectName CreateTempTableName(string tempTableSchema, ObjectName name) =>
			new($"{name.Schema}_{name.Name}", tempTableSchema);

		private static Association CreateTempAssociation(string tempTableSchema, Association association) =>
			new($"{tempTableSchema}_{association.Name}", association.SourceKey, association.TargetKey);

		private static OrderedGraph<TableSchema> FilterTables(
			OrderedGraph<TableSchema> tables,
			IReadOnlyCollection<OrderedItem<ITableContainer>> containers)
		{
			var names = containers
				.SelectMany(oc => GetTableNames(oc.Value))
				.ToHashSet();

			return tables.FilterDeep(t => names.Contains(t.Name));
		}

		private static IReadOnlyCollection<TableSchema> MapTables(
			string tempSchemaName,
			OrderedGraph<TableSchema> tables,
			IReadOnlyCollection<OrderedItem<ITableContainer>> containers) =>
			FilterTables(tables, containers)
				.Nodes
				.Select(ot => ot.Value)
				.MapDeep<TableSchema, TableSchema>(
					(r, t) => r.Map(_ => t, a => CreateTempAssociation(tempSchemaName, a)),
					(t, rs) =>
						new TableSchema(
							CreateTempTableName(tempSchemaName, t.Name),
							t.Columns,
							t.PrimaryKey,
							rs));

		private static OrderedItem<ITableContainer>[] MapContainers(
			string tempSchemaName,
			IReadOnlyCollection<OrderedItem<ITableContainer>> containers) =>
			containers.Select(oc => oc.Map<ITableContainer>(c => c switch
				{
					Table t => t.MapName(n => CreateTempTableName(tempSchemaName, n)),
					MutualRowGroup rg => rg.Map(
						n => CreateTempTableName(tempSchemaName, n),
						a => CreateTempAssociation(tempSchemaName, a)),
					MutualTableGroup tg => tg.Map(n => CreateTempTableName(tempSchemaName, n)),
					_ => throw new NotSupportedException(
						$"Unknown {nameof(ITableContainer)} type '{c.GetType().Name}'")
				}))
				.ToArray();

		private static ObjectName[] GetTableNames(ITableContainer container) =>
			container switch
			{
				Table t => new[] { t.Name },
				MutualRowGroup rg => rg.Tables.Select(t => t.Name).ToArray(),
				MutualTableGroup tg => tg.Tables.Select(t => t.Name).ToArray(),
				_ => throw new NotSupportedException(
					$"Unknown {nameof(ITableContainer)} type '{container.GetType().Name}'")
			};
	}
}