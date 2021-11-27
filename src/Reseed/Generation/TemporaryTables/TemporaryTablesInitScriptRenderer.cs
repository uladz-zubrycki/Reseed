using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Generation.Insertion;
using Reseed.Generation.Schema;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Schema;
using Reseed.Utils;
using static Reseed.Generation.ScriptRenderer;

namespace Reseed.Generation.TemporaryTables
{
	internal static class TemporaryTablesInitScriptRenderer
	{
		public static IReadOnlyCollection<OrderedItem<SqlScriptAction>> Render(
			[NotNull] string tempSchemaName,
			[NotNull] IReadOnlyCollection<TableSchema> tempTables,
			[NotNull] IReadOnlyCollection<OrderedItem<ITableContainer>> tempContainers)
		{
			if (tempSchemaName == null) throw new ArgumentNullException(nameof(tempSchemaName));
			if (tempTables == null) throw new ArgumentNullException(nameof(tempTables));
			if (tempContainers == null) throw new ArgumentNullException(nameof(tempContainers));
			if (tempContainers.Count == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(tempContainers));

			var foreignKeys = tempTables
				.SelectMany(t => t.GetRelations())
				.ToArray();

			return new List<SqlScriptAction>()
				.AddScript(new SqlScriptAction("Create temp schema", RenderCreateSchema(tempSchemaName)))
				.AddScript(new SqlScriptAction("Create temp tables", RenderCreateTables(tempTables)))
				.AddScriptWhen(
					() => new SqlScriptAction(
						"Create temp tables foreign keys",
						RenderCreateForeignKeys(foreignKeys)),
					foreignKeys.Length > 0)
				.AddScript(InsertScriptRenderer.Render(tempContainers).Map(_ => _, "Fill temp tables"))
				.WithNaturalOrder()
				.ToArray();
		}

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
					(c.IsIdentity
						? $"IDENTITY ({c.IdentitySeed}, {c.IdentityIncrement}) "
						: "") +
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
}