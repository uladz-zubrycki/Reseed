using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Data;
using Reseed.Ordering;
using Reseed.Schema;
using static Reseed.Ordering.OrderedItem;

namespace Reseed.Rendering.Schema
{
	internal static class TableBuilder
	{
		public static IReadOnlyCollection<Table> Build(
			[NotNull] IReadOnlyCollection<TableSchema> tables,
			[NotNull] IReadOnlyCollection<Entity> entities)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (entities == null) throw new ArgumentNullException(nameof(entities));

			var schemasMap = tables.ToDictionary(t => t.Name);

			var buildUnknownTableError =
				CreateUnknownTableErrorBuilder(tables);

			return entities
				.GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
				.Select(gr =>
				{
					var origins = gr.Select(e => e.Origin)
						.Distinct()
						.ToArray();

					var tableName = ParseTableName(gr.Key, origins);
					if (!schemasMap.TryGetValue(tableName, out var table))
					{
						throw buildUnknownTableError(tableName, origins);
					}

					var columns = table
						.Columns
						.Where(CouldInsertColumn)
						.Select(CreateColumn)
						.ToArray();

					var rows = CreateRows(
						table.Name,
						table.PrimaryKey,
						table.Columns,
						gr.ToArray());

					return new Table(
						new TableDefinition(table.Name, table.PrimaryKey, columns),
						rows);
				})
				.ToArray();
		}

		private static bool CouldInsertColumn(ColumnSchema columnSchema) =>
			!columnSchema.IsComputed;

		private static Column CreateColumn(ColumnSchema columnSchema)
		{
			return new Column(
				columnSchema.Order,
				columnSchema.Name,
				HasQuotedLiteral(columnSchema.DataType),
				!(columnSchema.IsNullable || columnSchema.HasDefaultValue),
				columnSchema.IsIdentity,
				columnSchema.IsPrimaryKey,
				null);

			static bool HasQuotedLiteral(DataType dataType) =>
				dataType.IsChar ||
				dataType.IsText ||
				dataType.IsDate ||
				dataType.IsTime ||
				dataType.IsDateTime ||
				dataType.IsDateTime2 ||
				dataType.IsDateTimeOffset ||
				dataType.IsUniqueIdentifier ||
				dataType.IsXml;
		}

		private static OrderedItem<Row>[] CreateRows(
			ObjectName tableName,
			Key primaryKey,
			IEnumerable<ColumnSchema> tableColumns,
			Entity[] entities)
		{
			var getColumn = BuildColumnProvider(tableColumns);

			return entities
				.Select((e, i) => Ordered(
					i,
					CreateRow(e, tableName, primaryKey, getColumn)))
				.ToArray();
		}

		private static Row CreateRow(
			Entity entity,
			ObjectName tableName,
			Key primaryKey,
			Func<Entity, Property, ColumnSchema> getColumn)
		{
			ValidateDuplicatedProperties(entity);

			(string column, string value)[] values = entity.Properties.Select(p =>
				{
					var column = getColumn(entity, p);
					if (column.IsComputed)
					{
						throw BuildComputedColumnException(entity, column);
					}

					return (p.Name, AdjustPropertyValue(p, column));
				})
				.ToArray();

			return new Row(tableName, primaryKey, entity.Origin, values);
		}

		private static Func<Entity, Property, ColumnSchema> BuildColumnProvider(
			IEnumerable<ColumnSchema> tableColumns)
		{
			var columnsMap = tableColumns
				.ToDictionary(c => c.Name.ToLowerInvariant());

			return (entity, property) =>
			{
				if (!columnsMap.TryGetValue(property.Name.ToLowerInvariant(), out var column))
				{
					throw BuildUnknownColumnException(property, entity);
				}

				return column;
			};
		}

		private static string AdjustPropertyValue(Property p, ColumnSchema column) =>
			column.DataType.IsBit
				? bool.TryParse(p.Value, out var boolean)
					? boolean ? "1" : "0"
					: p.Value
				: p.Value;

		private static void ValidateDuplicatedProperties(Entity entity)
		{
			var duplicates = entity.Properties
				.GroupBy(p => p.Name.ToLowerInvariant())
				.Where(gr => gr.Count() > 1)
				.Select(gr => gr.Key)
				.ToArray();

			if (duplicates.Length > 0)
			{
				var propertyMessage =
					(duplicates.Length == 1
						? $"Property '{duplicates[0]}' is"
						: $"Properties {string.Join(", ", duplicates.Select(d => $"'{d}'"))} are") +
					" defined multiple times for same entity. ";

				throw new InvalidOperationException(
					$"Invalid '{entity.Name}' entity test data. " +
					propertyMessage +
					BuildOriginErrorMessage(entity.Origin));
			}
		}

		private static Func<ObjectName, DataFile[], Exception> CreateUnknownTableErrorBuilder(
			IReadOnlyCollection<TableSchema> tables)
		{
			var schemaNamesByTableNameMap =
				tables.GroupBy(s => s.Name.Name.ToLowerInvariant())
					.ToDictionary(
						gr => gr.Key,
						gr => gr
							.Select(t => t.Name.Schema)
							.ToArray());

			return (tableName, origins) =>
			{
				var schemaNameMisprintMessage =
					schemaNamesByTableNameMap.TryGetValue(tableName.Name, out var schemaNames)
						? $". Table with name '{tableName.Name}' exists in schemas ['{string.Join(", ", schemaNames)}'], " +
						  $"make sure that specified schema '{tableName.Schema}' is the intended one"
						: "";

				return new InvalidOperationException(
					$"Can't find corresponding sql table for entity '{tableName.Name}'{schemaNameMisprintMessage}. " +
					$"{BuildOriginErrorMessage(origins)}");
			};
		}

		private static InvalidOperationException BuildComputedColumnException(Entity entity,
			ColumnSchema column) =>
			new(
				$"Invalid '{entity.Name}' entity test data. " +
				$"Table column '{column.Name}' is computed and shouldn't be specified. " +
				BuildOriginErrorMessage(entity.Origin));

		private static InvalidOperationException BuildUnknownColumnException(Property property, Entity entity) =>
			new(
				$"Invalid '{entity.Name}' entity test data. " +
				$"Can't find corresponding table column for property '{property.Name}'. " +
				"Make sure that all existing migrations are applied to database and it has up to date structure. " +
				BuildOriginErrorMessage(entity.Origin));

		private static ObjectName ParseTableName(string entityName, DataFile[] origins)
		{
			const string defaultSchemaName = "dbo";
			var parts = entityName.Split('.')
				.Select(s => s)
				.ToArray();

			return parts.Length switch
			{
				1 => new ObjectName(parts.Single(), defaultSchemaName),
				2 => new ObjectName(parts[1], parts[0]),
				_ => throw new InvalidOperationException(
					$"Can't parse table name from entity '{entityName}'. " +
					"Expected to have name as either 'TableName' or 'SchemaName.TableName'. " +
					$"{BuildOriginErrorMessage(origins)}")
			};
		}

		private static string BuildOriginErrorMessage(params DataFile[] origins) =>
			origins.Length == 1
				? $"Error in '{origins[0].Name}' test data file at '{origins[0].Path}'"
				: $"Error in multiple test data files: {string.Join(", ", origins.Select(o => $"'{o.Name}' at '{o.Path}'"))}";
	}
}