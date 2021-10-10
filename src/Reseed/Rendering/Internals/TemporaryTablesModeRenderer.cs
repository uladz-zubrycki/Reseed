using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Rendering.Internals.Decorators;
using Reseed.Rendering.Modes;
using Reseed.Schema;
using Reseed.Schema.Internals;
using Reseed.Utils;
using static Reseed.Ordering.OrderedItem;
using static Reseed.Rendering.Internals.CommonScriptNames;
using static Reseed.Rendering.Internals.ScriptRendererUtils;

namespace Reseed.Rendering.Internals
{
	internal static class TemporaryTablesModeRenderer
	{
	
	}

	internal static class TemporaryTablesInitScriptRenderer
	{
		public static IReadOnlyCollection<OrderedItem<DbScript>> Render(
			[NotNull] string tempSchemaName,
			[NotNull] IReadOnlyCollection<TableSchema> tempTables,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> tempContainers) =>
			OrderedCollection(
				new DbScript("Create temp schema", RenderCreateSchema(tempSchemaName)),
				new DbScript("Create temp tables", RenderCreateTables(tempTables)),
				new DbScript(
					"Create temp tables foreign keys",
					RenderCreateForeignKeys(tempTables.SelectMany(t => t.GetRelations()))),
				InsertScriptRenderer.Render(tempContainers).Map(_ => _, "Fill temp tables"));

		private static string RenderCreateTables(IReadOnlyCollection<TableSchema> tables) =>
			string.Join(Environment.NewLine + Environment.NewLine,
				tables.Select(table =>
				{
					var columnsScript = RenderColumns(table);
					var pkScript = RenderPrimaryKey(table);
					return $@"
						|CREATE TABLE {table.Name.GetSqlName()} (
						{columnsScript.WithMargin("\t", '|')},
						{(pkScript == null ? "" : pkScript.WithMargin("\t", '|'))}
						|)".TrimMargin('|');
				}));

		private static string RenderColumns(TableSchema table)
		{
			var columns = table.Columns.ToArray();

			return string.Join("," + Environment.NewLine,
				columns.Select(c =>
					$"[{c.Name}] {c.DataType} " +
					$"{(c.IsIdentity ? "IDENTITY (1, 1) " : "")}" +
					$"{(c.HasDefaultValue ? $"{RenderDefaultConstraint(c, table.Name.Name)} " : "")}" +
					$"{(c.IsNullable || c.IsComputed ? "NULL" : "NOT NULL")}"));
		}

		private static string RenderDefaultConstraint(ColumnSchema column, string tableName) =>
			$"CONSTRAINT [DF_{tableName}_{column.Name}] DEFAULT {column.DefaultValueExpression}";

		private static string RenderPrimaryKey(TableSchema table) =>
			table.PrimaryKey == null
				? null
				: $@"CONSTRAINT [PK_{table.Name.Name}] PRIMARY KEY CLUSTERED({RenderKey(table.PrimaryKey)})";
	}

	internal static class TemporaryTableInsertScriptRenderer
	{
		public static DbScript Render(
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] Func<ObjectName, ObjectName> mapTableName)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (mapTableName == null) throw new ArgumentNullException(nameof(mapTableName));

			return new DbScript(
				"Insert from temp tables",
				string.Join(Environment.NewLine + Environment.NewLine,
					MutualReferenceResolver.MergeChunks(
						tables,
						ts =>
							string.Join(Environment.NewLine + Environment.NewLine,
								ts.Order().Select(t =>
									RenderInsertFromTempTables(
										new[] { t },
										Array.Empty<Relation<TableSchema>>(),
										mapTableName))),
						ts =>
							RenderInsertFromTempTables(
								ts.Items.Order(),
								ts.Relations,
								mapTableName)).Order()));
		}

		private static string RenderInsertFromTempTables(
			IEnumerable<TableSchema> tables,
			IReadOnlyCollection<Relation<TableSchema>> foreignKeys,
			Func<ObjectName, ObjectName> mapTableName)
		{
			var fkDecorator = CreateForeignKeysDecorator(foreignKeys);
			return fkDecorator.Decorate(string.Join(Environment.NewLine + Environment.NewLine,
				tables.Select(t =>
				{
					var columnsScript = string.Join(", ", t.Columns
							.Where(c => !c.IsComputed)
							.Select(c => $"[{c.Name}]"))
						.Wrap(100, _ => _, _ => true, ',')
						.WithMargin("\t", '|');

					var identityDecorator = CreateIdentityDecorator(t);
					return identityDecorator.Decorate($@"
						|INSERT INTO {t.Name.GetSqlName()} WITH (TABLOCKX) (
						{columnsScript}
						|)
						|SELECT 
						{columnsScript}
						|FROM {mapTableName(t.Name).GetSqlName()}"
						.TrimMargin('|'));
				})));
		}

		private static IdentityInsertDecorator CreateIdentityDecorator(TableSchema table) =>
			new(
				table.Name,
				table.Columns.Any(c => c.IsIdentity));

		private static DisableForeignKeysDecorator CreateForeignKeysDecorator(
			IReadOnlyCollection<Relation<TableSchema>> foreignKeys) =>
			new(
				foreignKeys
					.Select(r => r.Map(t => t.Name))
					.ToArray());
	}

	internal static class TemporaryTablesInsertProcedureRenderer
	{
		public static IReadOnlyCollection<DbStep> GenerateInsertProcedureActions(
			[NotNull] ObjectName procedureName,
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] Func<ObjectName, ObjectName> mapTableName)
		{
			var script = TemporaryTableInsertScriptRenderer.Render(
				tables,
				mapTableName);

			var insertProcedure = script
				.Map(s => RenderCreateStoredProcedure(procedureName, s),
					CreateInsertSp);

			var dropInsertProcedure = RenderDropProcedureScript(
				DropInsertSp,
				procedureName);

			return new[]
			{
				new DbStep(DbActionStage.PrepareDb, OrderedCollection<IDbAction>(
					dropInsertProcedure,
					insertProcedure)),
				new DbStep(DbActionStage.Insert, OrderedCollection<IDbAction>(
					RenderExecuteProcedureScript(
						ExecuteInsertSp,
						procedureName))),
				new DbStep(DbActionStage.CleanupDb, OrderedCollection<IDbAction>(
					dropInsertProcedure))
			};
		}
	}

	internal static class TemporaryTablesSqlBulkCopyRenderer
	{
		private const SqlBulkCopyOptions DefaultOptions =
			SqlBulkCopyOptions.KeepIdentity |
			SqlBulkCopyOptions.KeepNulls |
			SqlBulkCopyOptions.TableLock;

		public static IReadOnlyCollection<OrderedItem<IDbAction>> GenerateActions(
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] Func<ObjectName, ObjectName> mapTableName,
			TemporaryTablesInsertSqlBulkCopyMode options)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));

			return tables.Nodes
				.Select(ot => ot.Map<IDbAction>(
					t =>
					{
						IReadOnlyCollection<ColumnSchema> columns = t.Columns
							.Where(c => !c.IsComputed)
							.ToArray();

						return new SqlBulkCopyAction(
							$"Insert into {t.Name.GetSqlName()}",
							RenderSelectScript(mapTableName(t.Name), columns),
							t.Name,
							options.CustomizeBulkCopy(DefaultOptions),
							columns
								.Select(c => new SqlBulkCopyColumnMapping(
									c.Name,
									c.Name))
								.ToArray());
					}))
				.ToArray();
		}

		private static string RenderSelectScript(
			ObjectName table,
			IReadOnlyCollection<ColumnSchema> columns)
		{
			var columnsScript =
				string.Join(", ", columns
					.OrderBy(c => c.Order)
					.Select(c => $"[{c.Name}]"));

			return $@"
				|SELECT 
				{columnsScript
					.Wrap(100, _ => _, _ => true, ',')
					.WithMargin("\t", '|')}
				|FROM {table.GetSqlName()}"
				.TrimMargin('|');
		}
	}

	internal static class TemporaryTablesCleanupScriptRenderer
	{
		public static IReadOnlyCollection<OrderedItem<DbScript>> Render(
			[NotNull] string tempSchemaName,
			[NotNull] IReadOnlyCollection<TableSchema> tables) =>
			OrderedCollection(
				new DbScript(
					"Drop temp tables foreign keys",
					RenderDropForeignKeys(
						tables.SelectMany(t => t.GetRelations()),
						true)),
				new DbScript("Drop temp tables", RenderDropTables(tables)),
				new DbScript("Drop temp schema", RenderDropSchema(tempSchemaName)));

		private static string RenderDropTables(IReadOnlyCollection<TableSchema> tables) =>
			string.Join(Environment.NewLine + Environment.NewLine,
				tables.Select(t => RenderDropTable(t.Name)));
	}
}