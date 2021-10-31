using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Dsl.TemporaryTables;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Rendering.Cleanup;
using Reseed.Rendering.Schema;
using Reseed.Schema;
using Reseed.Utils;
using static Reseed.Rendering.Scripts;

namespace Reseed.Rendering.TemporaryTables
{
	internal static class TemporaryTablesModeRenderer
	{
		public static SeedActions Render(
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers,
			[NotNull] TemporaryTablesMode mode)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (containers == null) throw new ArgumentNullException(nameof(containers));
			if (mode == null) throw new ArgumentNullException(nameof(mode));

			return new SeedActionsBuilder()
				.AddCleanup(mode.CleanupDefinition, tables)
				.Add(SeedStage.PrepareDb, RenderInit(
					mode.SchemaName,
					tables,
					containers).Order())
				.AddInsertFrom(
					mode.InsertDefinition,
					mode.SchemaName,
					tables,
					containers)
				.Add(SeedStage.CleanupDb, RenderDrop(
					mode.SchemaName,
					tables,
					containers).Order())
				.Build();
		}

		private static IReadOnlyCollection<OrderedItem<SqlScriptAction>> RenderInit(
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
			[NotNull] TemporaryTablesInsertDefinition options,
			[NotNull] string tempSchemaName,
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers)
		{
			return options switch
			{
				TemporaryTablesInsertScriptDefinition _ =>
					builder.Add(SeedStage.Insert,
						TemporaryTableInsertScriptRenderer.Render(
							FilterTables(tables, containers),
							n => CreateTempTableName(tempSchemaName, n))),

				TemporaryTablesInsertProcedureDefinition procedureOptions =>
					AddInsertProcedure(
						builder,
						procedureOptions.ProcedureName,
						FilterTables(tables, containers),
						n => CreateTempTableName(tempSchemaName, n)),

				TemporaryTablesInsertSqlBulkCopyDefinition bulkCopyOptions =>
					builder.Add(SeedStage.Insert,
						TemporaryTablesSqlBulkCopyRenderer.RenderInsert(
							FilterTables(tables, containers),
							n => CreateTempTableName(tempSchemaName, n),
							bulkCopyOptions)),

				_ => throw new NotSupportedException(
					$"Unknown {nameof(TemporaryTablesInsertDefinition)} '{options.GetType().FullName}' value")
			};
		}

		private static SeedActionsBuilder AddInsertProcedure(
			[NotNull] SeedActionsBuilder builder,
			[NotNull] ObjectName procedureName,
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] Func<ObjectName, ObjectName> mapTableName)
		{
			var createProcedure = TemporaryTableInsertScriptRenderer.Render(tables, mapTableName)
				.Map(s => RenderCreateStoredProcedure(procedureName, s), CommonScriptNames.CreateInsertSp);

			var dropProcedure = RenderDropProcedureScript(
				CommonScriptNames.DropInsertSp,
				procedureName);

			return builder
				.Add(SeedStage.PrepareDb, dropProcedure, createProcedure)
				.Add(SeedStage.Insert, RenderExecuteProcedureScript(
					CommonScriptNames.ExecuteInsertSp, procedureName))
				.Add(SeedStage.CleanupDb, dropProcedure);
		}

		private static IReadOnlyCollection<OrderedItem<SqlScriptAction>> RenderDrop(
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