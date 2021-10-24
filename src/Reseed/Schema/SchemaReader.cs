using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Reseed.Utils;
using static Reseed.Ordering.OrderedItem;

namespace Reseed.Schema
{
	// todo: Replace anons by dtos and extract methods
	internal static class SchemaReader
	{
		internal static IReadOnlyCollection<TableData> LoadTables(SqlConnection connection)
		{
			var columns = ExecuteQuery(
				connection,
				BuildColumnsQuery(),
				(r, cs) =>
				{
					var primaryKeyOrder = r.GetInt32(cs["PrimaryKeyColumnOrder"]);
					return new
					{
						SchemaName = r.GetString(cs["TableSchema"]),
						TableName = r.GetString(cs["TableName"]),
						TableId = r.GetInt32(cs["TableId"]),
						ColumnName = r.GetString(cs["ColumnName"]),
						ColumnOrder = r.GetInt32(cs["ColumnOrder"]),
						IsNullableColumn = r.GetInt32(cs["IsNullableColumn"]) == 1,
						ColumnDefaultValue = r.TryGet(cs["ColumnDefaultValue"], r.GetString),
						IsIdentityColumn = r.GetInt32(cs["IsIdentityColumn"]) == 1,
						IsComputedColumn = r.GetInt32(cs["IsComputedColumn"]) == 1,
						PrimaryKeyColumnOrder = primaryKeyOrder == -1 ? (int?) null : primaryKeyOrder,
						Type = new
						{
							Name = r.GetString(cs["ColumnTypeName"]),
							MaxLength = r.TryGetValue(cs["ColumnTypeMaxLength"], r.GetInt32),
							NumericPrecision = r.TryGetValue(cs["ColumnTypeNumericPrecision"], r.GetInt32),
							NumericPrecisionRadix = r.TryGetValue(cs["ColumnTypeNumericPrecisionRadix"], r.GetInt32),
							NumericScale = r.TryGetValue(cs["ColumnTypeNumericScale"], r.GetInt32),
							DateTimePrecision = r.TryGetValue(cs["ColumnTypeDateTimePrecision"], r.GetInt32),
						}
					};
				});

			return columns.GroupBy(c => new { c.TableName, c.SchemaName, c.TableId })
				.Select(gr =>
				{
					try
					{
						var primaryKeyColumns = gr
							.Where(k => k.PrimaryKeyColumnOrder != null)
							.Select(c => Ordered(c.PrimaryKeyColumnOrder.Value, c.ColumnName))
							.ToArray();

						return new TableData(
							new ObjectName(gr.Key.TableName, gr.Key.SchemaName),
							gr.Key.TableId,
							primaryKeyColumns.Any() ? new Key(primaryKeyColumns) : null,
							gr.Select(c => new ColumnSchema(
									c.ColumnOrder,
									c.ColumnName,
									new DataType(
										c.Type.Name,
										c.Type.MaxLength,
										c.Type.NumericPrecision ?? c.Type.DateTimePrecision,
										c.Type.NumericScale),
									c.PrimaryKeyColumnOrder != null,
									c.IsIdentityColumn,
									c.IsComputedColumn,
									c.IsNullableColumn,
									c.ColumnDefaultValue))
								.ToArray());
					}
					catch (Exception ex)
					{
						throw new Exception(
							$"{nameof(SchemaReader)} failed, unsupported schema for table {gr.Key}", ex);
					}
				})
				.ToArray();
		}

		internal static IReadOnlyCollection<Relation<TableData>> LoadForeignKeys(
			SqlConnection connection,
			IReadOnlyCollection<TableData> tables)
		{
			var tableNamesParameter = string.Join(", ",
				tables.Select(t => $"'{t.Name.Name}'"));

			var foreignKeysData = ExecuteQuery(
				connection,
				BuildForeignKeysQuery(tableNamesParameter),
				(r, cs) =>
					new
					{
						Id = r.GetInt32(cs["Id"]),
						Name = r.GetString(cs["Name"]),
						OriginId = r.GetInt32(cs["OriginId"]),
						ReferenceId = r.GetInt32(cs["ReferenceId"])
					});

			if (foreignKeysData.Count == 0)
			{
				return Array.Empty<Relation<TableData>>();
			}

			var columns = ExecuteQuery(
				connection,
				BuildForeignKeyColumnsQuery(foreignKeysData.Select(fk => fk.Id)),
				(r, cs) =>
					new
					{
						Name = r.GetString(cs["Name"]),
						IsOriginColumn = r.GetInt32(cs["IsOriginColumn"]) == 1,
						Order = r.GetInt32(cs["ColumnOrder"]),
						ForeignKeyId = r.GetInt32(cs["ForeignKeyId"])
					});

			var tablesById = tables.ToDictionary(t => t.ObjectId);
			var foreignKeysDataById = foreignKeysData.ToDictionary(fk => fk.Id);
			var foreignKeys =
				columns.GroupBy(k => k.ForeignKeyId)
					.Select(gr =>
					{
						var foreignKey = foreignKeysDataById[gr.Key];
						var (parentColumns, childColumns) =
							gr.PartitionBy(c => c.IsOriginColumn);

						return new Relation<TableData>(
							tablesById[foreignKey.OriginId],
							tablesById[foreignKey.ReferenceId],
							new Association(
								foreignKey.Name,
								new Key(parentColumns
									.Select(c => Ordered(c.Order, c.Name))
									.ToArray()),
								new Key(childColumns
									.Select(c => Ordered(c.Order, c.Name))
									.ToArray())));
					})
					.ToArray();

			return foreignKeys;
		}

		private static string BuildColumnsQuery() => @"
			|SELECT
			|	c.[table_schema] as TableSchema,
			|	c.[table_name] as TableName,
			|	t.[object_id] as TableId,
			|	c.[column_name] as ColumnName,
			|	c.[data_type] as ColumnTypeName,
			|	c.[ordinal_position] as ColumnOrder,
			|	CAST(c.[character_maximum_length] AS INT) as ColumnTypeMaxLength,
			|	CAST(c.[numeric_precision] AS INT) as ColumnTypeNumericPrecision,
			|	CAST(c.[numeric_precision_radix] AS INT) as ColumnTypeNumericPrecisionRadix, 
			|	CAST(c.[numeric_scale] AS INT) as ColumnTypeNumericScale, 
			|	CAST(c.[datetime_precision] AS INT) as ColumnTypeDateTimePrecision, 
			|	CASE WHEN c.[is_nullable] = 'yes' THEN 1 ELSE 0 END as IsNullableColumn,
			|	c.[column_default] as ColumnDefaultValue,
			|	columnproperty(t.[object_id], c.[column_name], 'IsIdentity') as IsIdentityColumn,
			|	columnproperty(t.[object_id], c.[column_name],'IsComputed') as IsComputedColumn,
			|	COALESCE((
			|		SELECT ic.[key_ordinal]
			|		FROM [sys].[indexes] AS i 
			|		JOIN [sys].[index_columns] AS ic 
			|		ON i.[object_id] = ic.[object_id] AND 
			|			i.[index_id] = ic.[index_id]
			|		WHERE 
			|			ic.[object_id] = t.[object_id] AND
			|			col_name(ic.[object_id], ic.[column_id]) = c.[column_name] AND
			|			i.[is_primary_key] = 1),
			|		-1) AS PrimaryKeyColumnOrder
			|FROM [information_schema].[columns] c
			|JOIN [sys].[tables] AS t ON 
			|	c.[table_name] = t.[name] AND
			|	c.[table_schema] = schema_name(t.[schema_id])		
			|WHERE t.[type_desc] = N'USER_TABLE'"
			.TrimMargin('|');

		private static string BuildForeignKeysQuery(string tableNamesParameter) => @"
			|SELECT 
			|	fk.[object_id] as Id,
			|	fk.[name] as [Name],
			|	fk.[parent_object_id] as OriginId, 
			|	fk.[referenced_object_id] as ReferenceId
			|FROM [sys].[foreign_keys] as fk
			|JOIN [sys].[tables] as tp
			|	ON fk.[parent_object_id] = tp.[object_id]
			|JOIN [sys].[tables] as tr
			|	ON fk.[referenced_object_id] = tr.[object_id]
			|WHERE tp.[name] IN (%Names%) AND tr.[name] IN (%Names%);"
			.Replace("%Names%", tableNamesParameter)
			.TrimMargin('|');

		private static string BuildForeignKeyColumnsQuery(IEnumerable<int> foreignKeyIds) => @"
			|SELECT 
			|	col.[name] as [Name],
			|	CASE 
			|		WHEN 
			|			col.[column_id] = fk.[parent_column_id] AND
			|			col.[object_id] = fk.[parent_object_id]
			|		THEN 1 
			|		ELSE 0 END as IsOriginColumn,
			|	fk.[constraint_object_id] as [ForeignKeyId],
			|	fk.[constraint_column_id] as [ColumnOrder]
			|FROM [sys].[foreign_key_columns] fk
			|JOIN [sys].[columns] col
			|ON 
			|	(col.[object_id] = fk.[parent_object_id] AND 
			|		col.[column_id] = fk.[parent_column_id]) OR
			|	(col.[object_id] = fk.[referenced_object_id] AND 
			|		col.[column_id] = fk.[referenced_column_id])
			|WHERE constraint_object_id IN (%FK_IDS%)"
			.Replace(
				"%FK_IDS%",
				string.Join(",", foreignKeyIds.Select(id => $"'{id}'")))
			.TrimMargin('|');

		private static IReadOnlyCollection<T> ExecuteQuery<T>(
			SqlConnection connection,
			string query,
			Func<SqlDataReader, IDictionary<string, int>, T> readItem)
		{
			using var command = connection.CreateCommand();
			command.CommandText = query;
			using var reader = command.ExecuteReader();

			var nameMapping = ArrayUtils
				.Init(reader.VisibleFieldCount, i => (name: reader.GetName(i), i))
				.ToDictionary(kv => kv.name, kv => kv.i);

			var items = new List<T>();
			while (reader.Read())
			{
				items.Add(readItem(reader, nameMapping));
			}

			return items;
		}
	}
}