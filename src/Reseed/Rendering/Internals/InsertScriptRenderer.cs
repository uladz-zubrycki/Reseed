﻿using System;
using System.Collections.Generic;
using System.Linq;
using Reseed.Ordering;
using Reseed.Rendering.Internals.Decorators;
using Reseed.Schema;
using Reseed.Utils;
using static Reseed.Ordering.OrderedItem;

namespace Reseed.Rendering.Internals
{
	internal static class InsertScriptRenderer
	{
		public static DbScript Render(
			IReadOnlyCollection<OrderedItem<ITableContainer>> containers)
		{
			if (containers == null) throw new ArgumentNullException(nameof(containers));
			if (containers.Count == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(containers));

			return new DbScript(
				CommonScriptNames.InsertScript,
				string.Join(Environment.NewLine + Environment.NewLine, containers
					.OrderBy(t => t.Order)
					.Select(t => RenderContainer(t.Value))));
		}

		private static string RenderContainer(ITableContainer container)
		{
			var decorators = GetScriptDecorators(container);
			var script = container switch
			{
				Table table => RenderTable(table),
				MutualRowGroup rows => RenderMutualTableGroup(rows),
				MutualTableGroup tables => RenderMutualTableGroup(tables),
				_ => throw new ArgumentOutOfRangeException(nameof(container))
			};

			return decorators.Aggregate(script, (s, o) => o.Decorate(s));
		}

		private static IScriptDecorator[] GetScriptDecorators(ITableContainer container)
		{
			var options = container switch
			{
				Table table => new IScriptDecorator[]
				{
					new IdentityInsertDecorator(
						table.Definition.Name,
						table.Columns.Any(c => c.IsIdentity)),
				},
				MutualRowGroup rows => new IScriptDecorator[]
				{
					new DisableForeignKeysDecorator(rows.ForeignKeys)
				},
				MutualTableGroup _ => Array.Empty<IScriptDecorator>(),
				_ => throw new ArgumentOutOfRangeException(nameof(container))
			};

			return options;
		}

		private static string RenderMutualTableGroup(MutualTableGroup tableGroup)
		{
			var scripts = tableGroup
				.GetTables()
				.OrderBy(o => o.Order)
				.Select(o => RenderContainer(o.Value));

			return string.Join(Environment.NewLine, scripts);
		}

		private static string RenderTable(Table table)
		{
			var getColumnByName = BuildColumnProvider(table.Definition);
			var getIdentityGenerator = BuildIdentityGeneratorProvider(table);

			var requiredColumns = table.Columns
				.Where(c => c.IsRequired)
				.ToArray();

			var groups = GroupRowsByColumns(table.Rows)
				.OrderBy(gr => gr.Order)
				.Select(gr =>
				{
					var groupColumns = gr.Value.columns
						.Select(getColumnByName)
						.Concat(requiredColumns)
						.Distinct()
						.ToArray();

					var columnsScript = RenderColumns(groupColumns);
					var rowsScript = RenderRows(
						table.Name,
						groupColumns,
						gr.Value.rows,
						getIdentityGenerator);

					const int maxLineLength = 100;
					return @$"
						|INSERT INTO {table.Name.GetSqlName()} WITH (TABLOCKX) (
						{columnsScript
							.Wrap(maxLineLength, _ => _, _ => true, ',')
							.WithMargin("\t", '|')}
						)
						|VALUES 
						{rowsScript
							.MergeLines(maxLineLength, " ")
							.Wrap(maxLineLength,
								s => s.WithMargin("\t"),
								line => line.Count(c => c == '\'') % 2 == 0,
								',')
							.WithMargin("\t", '|')}"
						.TrimMargin('|');
				});

			return string.Join(Environment.NewLine, groups);
		}

		private static Func<Column, IdentityGenerator> BuildIdentityGeneratorProvider(Table table)
		{
			var generatorsMap = table.Columns
				.Where(c => c.IsIdentity)
				.Select(c =>
				{
					var values = table.Rows
						.Select(r =>
							r.Value.GetValue(c.Name) is { } value && int.TryParse(value, out var number)
								? number
								: (int?) null)
						.Where(v => v != null)
						.Cast<int>()
						.ToArray();

					return (column: c, generator: new IdentityGenerator(values));
				})
				.ToDictionary(pair => pair.column, pair => pair.generator);

			return c => generatorsMap.TryGetValue(c, out var generator)
				? generator
				: throw BuildTableError(table.Name, $"Can't find identity generator for column with name '{c.Name}'");
		}

		private static OrderedItem<(string[] columns, OrderedItem<Row>[] rows)>[] GroupRowsByColumns(
			IReadOnlyCollection<OrderedItem<Row>> rows) =>
			rows
				.GroupBy(r => r.Value.Columns.ToArray(),
					InlineEqualityComparer<string[]>.Create(cs => string.Join(";",  cs)))
				.Select(gr => (columns: gr.Key, rows: gr.ToArray()))
				.OrderBy(gr => gr.rows.Select(r => r.Order).Min())
				.Select((gr, i) => Ordered(i, gr))
				.ToArray();

		private static string RenderColumns(IEnumerable<Column> columns) =>
			string.Join(", ", columns
				.OrderBy(c => c.Order)
				.Select(c => $"[{c.Name}]"));

		private static string RenderRows(
			ObjectName tableName,
			Column[] columns,
			IEnumerable<OrderedItem<Row>> rows,
			Func<Column, IdentityGenerator> getIdentityGenerator)
		{
			var rowValues = rows
				.OrderBy(r => r.Order)
				.Select(row =>
				{
					var renderedValues = columns
						.OrderBy(c => c.Order)
						.Select(c => GetColumnValue(tableName, c, row.Value, getIdentityGenerator))
						.Where(c => c != null)
						.Select(RenderValue);

					return $"({string.Join(", ", renderedValues)})";
				});

			return string.Join($",{Environment.NewLine}", rowValues);
		}

		private static ColumnValue GetColumnValue(
			ObjectName tableName,
			Column column,
			Row row,
			Func<Column, IdentityGenerator> getIdentityGenerator)
		{
			var value = row.GetValue(column.Name);
			if (value != null)
			{
				return new ColumnValue(column, value);
			}

			if (!column.IsRequired)
			{
				return null;
			}

			if (column.IsIdentity)
			{
				var generator = getIdentityGenerator(column);
				return new ColumnValue(column, generator.NextValue());
			}
			else
			{
				return column.DefaultValue != null
					? new ColumnValue(column, column.DefaultValue)
					: throw BuildTableError(tableName,
						$"Column '{column.Name}' is required and doesn't have default value, " +
						"but value is not provided");
			}
		}

		private static string RenderValue(ColumnValue value)
		{
			return value.Value == null ? "NULL"
				: value.Column.HasQuotedLiteral ? $"'{EscapeValue(value.Value)}'"
				: value.Value;

			static string EscapeValue(string s) => s.Replace("'", "''");
		}

		private static Func<string, Column> BuildColumnProvider(TableDefinition table)
		{
			var nameMapping =
				table.Columns.ToDictionary(c => c.Name, c => c);

			return columnName => nameMapping.TryGetValue(columnName, out var column)
				? column
				: throw BuildTableError(table.Name,
					$"Can't find column by name '{columnName}'. " +
					$"Known columns are {string.Join(", ", table.Columns.Select(c => $"'{c.Name}'"))}");
		}

		private static InvalidOperationException BuildTableError(ObjectName tableName, string error) =>
			new($"Can't render insert script for table '{tableName}'. " + error);
	}
}