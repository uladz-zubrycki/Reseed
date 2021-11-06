using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Configuration;
using Reseed.Data;
using Reseed.Data.FileSystem;
using Reseed.Execution;
using Reseed.Extension;
using Reseed.Generation;
using Reseed.Generation.Schema;
using Reseed.Graphs;
using Reseed.Ordering;
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

		public SeedActions Generate(
			[NotNull] SeedMode mode,
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
			return SeedActionGenerator.Generate(orderedSchemas, containers, mode);
		}

		public void Execute([NotNull] IReadOnlyCollection<OrderedItem<ISeedAction>> actions)
		{
			SeedActionExecutor.Execute(this.connectionString, actions);
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