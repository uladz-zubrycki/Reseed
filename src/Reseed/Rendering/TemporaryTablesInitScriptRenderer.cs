using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Rendering
{
	internal static class TemporaryTablesInitScriptRenderer
	{
		public static IReadOnlyCollection<OrderedItem<DbScript>> Render(
			[NotNull] string tempSchemaName,
			[NotNull] IReadOnlyCollection<TableSchema> tempTables,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> tempContainers) =>
			OrderedItem.OrderedCollection(
				new DbScript("Create temp schema", ScriptRendererUtils.RenderCreateSchema(tempSchemaName)),
				new DbScript("Create temp tables", RenderCreateTables(tempTables)),
				new DbScript(
					"Create temp tables foreign keys",
					ScriptRendererUtils.RenderCreateForeignKeys(tempTables.SelectMany(t => NodeExtensions.GetRelations<TableSchema>(t)))),
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
				: $@"CONSTRAINT [PK_{table.Name.Name}] PRIMARY KEY CLUSTERED({ScriptRendererUtils.RenderKey(table.PrimaryKey)})";
	}
}