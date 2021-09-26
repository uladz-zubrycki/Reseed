﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Data;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Rendering;
using Reseed.Rendering.Internals;
using Reseed.Schema;
using Reseed.Validation;

namespace Reseed
{
	public sealed class Seeder
	{
		// todo: refactor api to support independent Insert/Delete actions rendering
		public DbActions Generate(
			[NotNull] RenderMode mode,
			[NotNull] string connectionString, 
			[NotNull] string dataFolder)
		{
			if (mode == null) throw new ArgumentNullException(nameof(mode));
			if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
			if (dataFolder == null) throw new ArgumentNullException(nameof(dataFolder));

			IReadOnlyCollection<Entity> entities = DataReader.LoadData(dataFolder);
			IReadOnlyCollection<TableSchema> schemas = SchemaProvider.LoadSchema(connectionString);
			IReadOnlyCollection<Table> tables = TableBuilder.Build(schemas, entities);
			DataValidator.Validate(tables);

			OrderedGraph<TableSchema> orderedSchemas = NodeOrderer<TableSchema>.Order(schemas);
			IReadOnlyCollection<OrderedItem<ITableContainer>> containers = TableOrderer.Order(tables, orderedSchemas);
			return ScriptRenderer.Render(orderedSchemas, containers, mode);
		}

		public void Execute(
			[NotNull] IReadOnlyCollection<OrderedItem<IDbAction>> actions,
			[NotNull] string connectionString)
		{
			if (actions == null) throw new ArgumentNullException(nameof(actions));
			if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

			using var connection = new SqlConnection(connectionString);
			connection.Open();

			foreach (IDbAction dbAction in actions.Order())
			{
				try
				{
					if (dbAction is DbScript script)
					{
						ExecuteScript(connection, script);
					}
					else if (dbAction is SqlBulkCopyAction bulkCopy)
					{
						ExecuteSqlBulkCopy(connection, bulkCopy);
					}
					else
					{
						throw new NotSupportedException(
							$"Unknown {nameof(IDbAction)} type {dbAction.GetType().FullName}");
					}
				}
				catch (SqlException ex)
				{
					throw new InvalidOperationException(
						$"Error on '{dbAction.Name}' action execution", ex);
				}
			}
		}

		private static void ExecuteScript(SqlConnection connection, DbScript script)
		{
			using SqlCommand command = connection.CreateCommand();
			command.CommandText = script.Text;
			command.ExecuteNonQuery();
		}

		private static void ExecuteSqlBulkCopy(
			SqlConnection connection,
			SqlBulkCopyAction action)
		{
			using var bulkCopy = new SqlBulkCopy(
				connection.ConnectionString,
				action.Options)
			{
				DestinationTableName = action.DestinationTable.GetSqlName()
			};

			foreach (SqlBulkCopyColumnMapping mapping in action.Columns)
			{
				bulkCopy.ColumnMappings.Add(mapping);
			}

			using DbCommand command = connection.CreateCommand();
			command.CommandText = action.SourceScript;
			using DbDataReader reader = command.ExecuteReader();
			bulkCopy.WriteToServer(reader);
		}
	}
}