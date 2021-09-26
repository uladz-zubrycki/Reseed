using System;
using System.Collections.Generic;
using System.Linq;
using Reseed.Rendering;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Validation
{
	internal static class DataValidator
	{
		public static void Validate(IReadOnlyCollection<Table> tables)
		{
			ValidatePrimaryKeysUnique(tables);
		}

		private static void ValidatePrimaryKeysUnique(IReadOnlyCollection<Table> tables)
		{
			(TableDefinition table, Row[][] rows)[] duplicates =
				tables
					.Where(t => t.Definition.PrimaryKey != null)
					.Select(t =>
					{
						Row[][] rows = t.Rows
							.Select(or => (row: or.Value, key: or.Value.GetValue(t.Definition.PrimaryKey)))
							.Where(x => x.key.HasValue)
							.GroupBy(x => x.key)
							.Where(gr => gr.Count() > 1)
							.Select(gr => gr.Select(x => x.row).ToArray())
							.ToArray();

						return (table: t.Definition, rows);
					})
					.Where(t => t.rows.Any())
					.ToArray();

			if (duplicates.Any())
			{
				string messages = duplicates.Select(x =>
					{
						string errors = x.rows
							.Select(rs => RenderKeyDuplicates(x.table.PrimaryKey, rs))
							.JoinStrings(" and ");

						return $"Table {x.table.Name} has {errors}";
					})
					.JoinStrings("; ");

				throw new InvalidOperationException(
					$"Test data is invalid, there are duplicated primary keys. {messages}");
			}

			static string RenderKeyDuplicates(Key primaryKey, Row[] rows)
			{
				KeyValue value = rows.First().GetValue(primaryKey);

				string origins = rows
					.Select(r => r.Origin)
					.Distinct()
					.Select(x => x.ToString())
					.JoinStrings(", ");

				return $"{rows.Length} entities with key '{value}' defined in {origins}";
			}
		}
	}
}
