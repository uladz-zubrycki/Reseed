using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Rendering.Modes;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Rendering
{
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
}