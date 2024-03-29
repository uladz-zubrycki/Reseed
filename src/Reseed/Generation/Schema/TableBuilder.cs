﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Data;
using Reseed.Ordering;
using Reseed.Schema;
using static Reseed.Ordering.OrderedItem;

namespace Reseed.Generation.Schema
{
	internal static class TableBuilder
	{
		public static IReadOnlyCollection<Table> Build(
			[NotNull] IReadOnlyCollection<TableSchema> schemas,
			[NotNull] IReadOnlyCollection<Entity> entities)
		{
			if (schemas == null) throw new ArgumentNullException(nameof(schemas));
			if (entities == null) throw new ArgumentNullException(nameof(entities));

			var schemaByTableNameMap = schemas.ToDictionary(t => t.Name);
			var buildUnknownTableError = CreateUnknownTableErrorBuilder(schemas);

			return MatchEntityWithTableSchema(entities, schemaByTableNameMap, buildUnknownTableError)
				.GroupBy(x => x.TableSchema)
				.Select(gr =>
				{
					var tableSchema = gr.Key;
					return CreateTable(
						tableSchema, 
						gr.Select(x => x.Entity).ToArray());
				})
				.ToArray();
		}

		private static IEnumerable<(Entity Entity, TableSchema TableSchema)> MatchEntityWithTableSchema(
			IReadOnlyCollection<Entity> entities, 
			Dictionary<ObjectName, TableSchema> schemaByTableNameMap, 
			Func<ObjectName, EntityOrigin[], Exception> buildUnknownTableError) =>
			entities
				.GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
				.SelectMany(gr =>
				{
					var origins = gr.Select(e => e.Origin)
						.Distinct()
						.ToArray();

					var tableName = ParseTableName(gr.Key, origins);
					if (!schemaByTableNameMap.TryGetValue(tableName, out var table))
					{
						throw buildUnknownTableError(tableName, origins);
					}

					return gr.Select(e => (entity: e, table));
				});

		private static Table CreateTable(TableSchema table, Entity[] entities)
		{
			var columns = table
				.Columns
				.Where(CouldInsertColumn)
				.Select(CreateColumn)
				.ToArray();

			var tableDefinition = new TableDefinition(table.Name, table.PrimaryKey, columns);
			var rows = CreateRows(tableDefinition, entities);

			return new Table(tableDefinition, rows);
		}

		private static bool CouldInsertColumn(ColumnSchema columnSchema) =>
			!columnSchema.IsComputed;

		private static Column CreateColumn(ColumnSchema columnSchema)
		{
			return new Column(
				columnSchema.Order,
				columnSchema.Name,
				columnSchema.DataType,
				HasQuotedLiteral(columnSchema.DataType),
				!(columnSchema.IsNullable || columnSchema.HasDefaultValue),
				columnSchema.IsIdentity
					? new IdentityOptions(
						columnSchema.IdentitySeed!.Value, 
						columnSchema.IdentityIncrement!.Value)
					: null,
				columnSchema.IsPrimaryKey,
				columnSchema.IsComputed,
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
			TableDefinition tableDefinition,
			Entity[] entities)
		{
			var getColumn = BuildColumnProvider(tableDefinition.Columns);

			return entities
				.Select((e, i) => Ordered(
					i,
					CreateRow(e, tableDefinition, getColumn)))
				.ToArray();
		}

		private static Row CreateRow(
			Entity entity,
			TableDefinition tableDefinition,
			Func<Entity, Property, Column> getColumn)
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

			return new Row(tableDefinition, entity.Origin, values);
		}

		private static Func<Entity, Property, Column> BuildColumnProvider(
			IEnumerable<Column> tableColumns)
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

		private static string AdjustPropertyValue(Property p, Column column) =>
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
					" defined multiple times for the same entity. ";

				throw new InvalidOperationException(
					$"Invalid '{entity.Name}' entity test data. " +
					propertyMessage +
					BuildOriginErrorMessage(entity.Origin));
			}
		}

		private static Func<ObjectName, EntityOrigin[], Exception> CreateUnknownTableErrorBuilder(
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
					schemaNamesByTableNameMap.TryGetValue(tableName.Name.ToLowerInvariant(), out var schemaNames)
						? $". Table with name '{tableName.Name}' exists in schemas ['{string.Join(", ", schemaNames)}'], " +
						  $"make sure that specified schema '{tableName.Schema}' is the intended one"
						: "";

				return new InvalidOperationException(
					"Some of the entities provided don't have matching database tables. " +
					"Make sure that names are correct and expected. " +
					$"Can't find corresponding sql table for entity name '{tableName.Name}'{schemaNameMisprintMessage}. " +
					$"{BuildOriginErrorMessage(origins)}");
			};
		}

		private static InvalidOperationException BuildComputedColumnException(Entity entity,
			Column column) =>
			new(
				$"Invalid '{entity.Name}' entity data. " +
				$"Table column '{column.Name}' is computed and shouldn't be specified. " +
				BuildOriginErrorMessage(entity.Origin));

		private static InvalidOperationException BuildUnknownColumnException(Property property, Entity entity) =>
			new(
				$"Invalid '{entity.Name}' entity test data. " +
				$"Can't find corresponding table column for property '{property.Name}'. " +
				"Make sure that all existing migrations are applied to database and it has up to date structure. " +
				BuildOriginErrorMessage(entity.Origin));

		private static ObjectName ParseTableName(string entityName, EntityOrigin[] origins)
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
					$"Can't parse table name from entity name '{entityName}'. " +
					"Expected to have name as either 'TableName' or 'SchemaName.TableName'. " +
					$"{BuildOriginErrorMessage(origins)}")
			};
		}

		private static string BuildOriginErrorMessage(params EntityOrigin[] origins) =>
			origins.Length == 1
				? $"Error in data from origin \"{origins[0]}\""
				: $"Error in data from origins {string.Join(", ", origins.Select(o => '"' + o.ToString() + '"'))}";
	}
}