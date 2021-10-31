using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Data;
using Reseed.Data.FileSystem;
using Reseed.Dsl;
using Reseed.Extending;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Rendering;
using Reseed.Rendering.Schema;
using Reseed.Schema;
using Reseed.Validation;

namespace Reseed
{
	[PublicAPI]
	public sealed class Reseeder
	{
		private readonly string connectionString;

		public Reseeder([NotNull] string connectionString)
		{
			this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
		}

		// todo: refactor api to support independent Insert/Delete actions rendering
		public SeedActions Generate(
			[NotNull] RenderMode mode,
			[NotNull] IDataProvider dataProvider)
		{
			if (mode == null) throw new ArgumentNullException(nameof(mode));
			if (dataProvider == null) throw new ArgumentNullException(nameof(dataProvider));

			var entities = GetEntities(dataProvider);
			var schemas = LoadSchemas();
			var tables = TableBuilder.Build(schemas, entities);
			var extendedTables = TableExtender.Extend(tables);
			DataValidator.Validate(extendedTables);

			var orderedSchemas = NodeOrderer<TableSchema>.Order(schemas);
			var containers = TableOrderer.Order(extendedTables, orderedSchemas);
			return Renderer.Render(orderedSchemas, containers, mode);
		}

		public void Execute([NotNull] IReadOnlyCollection<OrderedItem<ISeedAction>> actions)
		{
			if (actions == null) throw new ArgumentNullException(nameof(actions));
			if (actions.Count == 0)
			{
				return;
			}

			using var connection = new SqlConnection(connectionString);
			connection.Open();

			foreach (var action in actions.Order())
			{
				try
				{
					switch (action)
					{
						case SqlScriptAction script:
							ExecuteScript(connection, script);
							break;
						case SqlBulkCopyAction bulkCopy:
							ExecuteSqlBulkCopy(connection, bulkCopy);
							break;
						default:
							throw new NotSupportedException(
								$"Unknown {nameof(ISeedAction)} type {action.GetType().FullName}");
					}
				}
				catch (SqlException ex)
				{
					throw new SeedActionExecutionException(action, ex);
				}
			}
		}

		private IReadOnlyCollection<Entity> GetEntities(IDataProvider dataProvider)
		{
			if (dataProvider is IVerboseDataProvider verboseProvider)
			{
				var loadResult = verboseProvider.GetEntitiesDetailed();
				if (loadResult.Entities.Count == 0)
				{
					throw BuildNoEntitiesException(verboseProvider, loadResult);
				}

				return loadResult.Entities;
			}
			else
			{
				var entities = dataProvider.GetEntities();
				if (entities.Count == 0)
				{
					throw BuildNoEntitiesException(dataProvider);
				}

				return entities;
			}
		}

		private IReadOnlyCollection<TableSchema> LoadSchemas()
		{
			var schemas = MsSqlSchemaProvider.LoadSchema(connectionString);
			if (schemas.Count == 0)
			{
				throw BuildNoSchemasException();
			}

			return schemas;
		}

		private static void ExecuteScript(SqlConnection connection, SqlScriptAction scriptAction)
		{
			using var command = connection.CreateCommand();
			command.CommandText = scriptAction.Text;
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

		private InvalidOperationException BuildNoEntitiesException(IDataProvider dataProvider) => 
			new(BuildDataProviderNoEntitiesErrorMessage(dataProvider));

		private InvalidOperationException BuildNoEntitiesException(
			IVerboseDataProvider dataProvider,
			EntityLoadResult loadResult)
		{
			return new InvalidOperationException(
				BuildDataProviderNoEntitiesErrorMessage(dataProvider) +
				BuildSourcesMessage(". Loaded", loadResult.LoadedSources) +
				BuildSourcesMessage(". Skipped", loadResult.SkippedSources));

			static string BuildSourcesMessage(string sourcesName, IReadOnlyCollection<DataFile> sources) =>
				sources.Count > 0
					? $"{sourcesName} sources are {string.Join(", ", sources.Select(s => s.ToString()))}"
					: string.Empty;
		}

		private static string BuildDataProviderNoEntitiesErrorMessage(IDataProvider dataProvider) =>
			$"The specified {nameof(IDataProvider)} wasn't able to find any entities, " +
			"while at least one is required. " +
			$"Make sure {dataProvider.GetType().Name} configuration is correct";

		private static InvalidOperationException BuildNoSchemasException() =>
			new("The specified database doesn't contain any tables, " +
			    $"therefore can't be used as {nameof(Reseeder)} target. " +
			    "Make sure that all the database migrations are properly applied if any" +
			    $"before using {nameof(Reseeder)}");
	}
}