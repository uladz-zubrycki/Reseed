using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Rendering.Dsl;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Rendering
{
	internal static class TemporaryTablesModeRenderer
	{
		public static DbActions Render(
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers,
			[NotNull] TemporaryTablesMode mode)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (containers == null) throw new ArgumentNullException(nameof(containers));
			if (mode == null) throw new ArgumentNullException(nameof(mode));

			return new DbActionsBuilder()
				.AppendCleanupActions(tables, mode.CleanupMode)
				.Append(DbActionStage.PrepareDb, RenderInit(
					mode.SchemaName,
					tables,
					containers).Order())
				.Append(RenderInsertFrom(
					mode.InsertMode,
					mode.SchemaName,
					tables,
					containers))
				.Append(DbActionStage.CleanupDb, RenderDrop(
					mode.SchemaName,
					tables,
					containers).Order())
				.Build();
		}

		private static IReadOnlyCollection<OrderedItem<DbScript>> RenderInit(
			[NotNull] string tempSchemaName,
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers)
		{
			return TemporaryTablesInitScriptRenderer.Render(
				tempSchemaName,
				MapTables(tempSchemaName, tables, containers),
				MapContainers(tempSchemaName, containers));
		}

		private static IReadOnlyCollection<DbStep> RenderInsertFrom(
			[NotNull] TemporaryTablesInsertMode options,
			[NotNull] string tempSchemaName,
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> containers)
		{
			return options switch
			{
				TemporaryTablesInsertScriptMode _ => new[]
				{
					new DbStep(
						DbActionStage.Insert,
						OrderedItem.OrderedCollection<IDbAction>(
							TemporaryTableInsertScriptRenderer.Render(
								FilterTables(tables, containers),
								n => CreateTempTableName(tempSchemaName, n))))
				},

				TemporaryTablesInsertProcedureMode procedureOptions =>
					TemporaryTablesInsertProcedureRenderer.GenerateInsertProcedureActions(
						procedureOptions.ProcedureName,
						FilterTables(tables, containers),
						n => CreateTempTableName(tempSchemaName, n)),

				TemporaryTablesInsertSqlBulkCopyMode bulkCopyOptions => new[]
				{
					new DbStep(
						DbActionStage.Insert,
						TemporaryTablesSqlBulkCopyRenderer.GenerateActions(
							FilterTables(tables, containers),
							n => CreateTempTableName(tempSchemaName, n),
							bulkCopyOptions))
				},

				_ => throw new NotSupportedException(
					$"Unknown {nameof(TemporaryTablesInsertMode)} '{options.GetType().FullName}' value")
			};
		}

		public static IReadOnlyCollection<OrderedItem<DbScript>> RenderDrop(
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
				.SelectMany(oc => oc.Value.TableNames)
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
					(r, t) =>
						r.Map(_ => t, a => CreateTempAssociation(tempSchemaName, a)),
					(t, rs) =>
						new TableSchema(
							CreateTempTableName(tempSchemaName, t.Name),
							t.Columns,
							t.PrimaryKey,
							rs));

		private static OrderedItem<ITableContainer>[] MapContainers(
			string tempSchemaName,
			IReadOnlyCollection<OrderedItem<ITableContainer>> containers) =>
			containers
				.Select(oc => oc.Map(
					c => c.MapTableName(n => CreateTempTableName(tempSchemaName, n))))
				.ToArray();
	}
}