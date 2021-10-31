using System;
using System.Collections.Generic;
using System.Linq;
using Reseed.Generation.Schema;
using Reseed.Internals.Utils;
using Reseed.Ordering;
using Reseed.Schema;

namespace Reseed.Generation.Insertion
{
	internal static class InsertScriptRenderer
	{
		public static SqlScriptAction Render(
			IReadOnlyCollection<OrderedItem<ITableContainer>> containers)
		{
			if (containers == null) throw new ArgumentNullException(nameof(containers));
			if (containers.Count == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(containers));

			return new SqlScriptAction(
				ScriptNames.InsertScript,
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

		private static IScriptDecorator[] GetScriptDecorators(ITableContainer container) =>
			container switch
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

		private static string RenderMutualTableGroup(MutualTableGroup tableGroup) =>
			string.Join(Environment.NewLine, tableGroup
				.GetTables()
				.OrderBy(o => o.Order)
				.Select(o => RenderContainer(o.Value)));

		private static string RenderTable(Table table)
		{
			var getColumnByName = BuildColumnProvider(table.Definition);
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
						gr.Value.rows);

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

		private static IEnumerable<OrderedItem<(IReadOnlyCollection<string> columns, Row[] rows)>> 
			GroupRowsByColumns(
			IReadOnlyCollection<OrderedItem<Row>> rows)
		{
			var comparer = InlineEqualityComparer<IReadOnlyCollection<string>>.Create(
				cs => string.Join(";", cs));
			var orderedRows = rows.Order().ToArray();
			var groups = new List<(IReadOnlyCollection<string>, Row[])>();
			var firstRow = orderedRows.First();
			var currentColumns = firstRow.Columns;
			var currentGroup = new List<Row> {firstRow};

			foreach (var row in orderedRows.Skip(1))
			{
				if (comparer.Equals(currentColumns, row.Columns))
				{
					currentGroup.Add(row);
				}
				else
				{
					groups.Add((currentColumns, currentGroup.ToArray()));
					currentColumns = row.Columns;
					currentGroup = new List<Row> {row};
				}
			}

			if (currentGroup.Count > 0)
			{
				groups.Add((currentColumns, currentGroup.ToArray()));
			}

			return groups
				.WithNaturalOrder()
				.ToArray();
		}

		private static string RenderColumns(IEnumerable<Column> columns) =>
			string.Join(", ", columns
				.OrderBy(c => c.Order)
				.Select(c => $"[{c.Name}]"));

		private static string RenderRows(
			ObjectName tableName,
			Column[] columns,
			IReadOnlyCollection<Row> rows)
		{
			var rowValues = rows
				.Select(row =>
				{
					var renderedValues = columns
						.OrderBy(c => c.Order)
						.Select(c => GetColumnValue(tableName, c, row))
						.Where(c => c != null)
						.Select(RenderValue);

					return $"({string.Join(", ", renderedValues)})";
				});

			return string.Join($",{Environment.NewLine}", rowValues);
		}

		private static ColumnValue GetColumnValue(
			ObjectName tableName,
			Column column,
			Row row)
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

			return column.DefaultValue != null
				? new ColumnValue(column, column.DefaultValue)
				: throw BuildTableError(tableName,
					$"Column '{column.Name}' is required and doesn't have default value, " +
					"but value is not provided");
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