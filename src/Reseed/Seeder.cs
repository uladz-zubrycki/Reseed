using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using JetBrains.Annotations;
using Reseed.Data;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Rendering;
using Reseed.Rendering.Modes;
using Reseed.Schema;
using Reseed.Validation;

namespace Reseed
{
	[PublicAPI]
	public sealed class Seeder
	{
		private readonly string connectionString;

		public Seeder([NotNull] string connectionString)
		{
			this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
		}

		// todo: refactor api to support independent Insert/Delete actions rendering
		public DbActions Generate(
			[NotNull] RenderMode mode,
			[NotNull] string dataFolder)
		{
			if (mode == null) throw new ArgumentNullException(nameof(mode));
			if (dataFolder == null) throw new ArgumentNullException(nameof(dataFolder));

			var entities = XmlDataReader.LoadData(dataFolder);
			var schemas = MsSqlSchemaProvider.LoadSchema(connectionString);
			var tables = TableBuilder.Build(schemas, entities);
			DataValidator.Validate(tables);

			var orderedSchemas = NodeOrderer<TableSchema>.Order(schemas);
			var containers = TableOrderer.Order(tables, orderedSchemas);
			return Renderer.Render(orderedSchemas, containers, mode);
		}

		public void Execute([NotNull] IReadOnlyCollection<OrderedItem<IDbAction>> actions)
		{
			if (actions == null) throw new ArgumentNullException(nameof(actions));
			if (actions.Count == 0)
			{
				return;
			}

			using var connection = new SqlConnection(connectionString);
			connection.Open();

			foreach (var dbAction in actions.Order())
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
			using var command = connection.CreateCommand();
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

			foreach (var mapping in action.Columns)
			{
				bulkCopy.ColumnMappings.Add(mapping);
			}

			using DbCommand command = connection.CreateCommand();
			command.CommandText = action.SourceScript;
			using var reader = command.ExecuteReader();
			bulkCopy.WriteToServer(reader);
		}
	}
}