using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Data.Providers;

namespace Reseed.Data
{
	internal static class DataProvider
	{
		public static IReadOnlyCollection<Entity> Load([NotNull] IReadOnlyCollection<IDataProvider> dataProviders)
		{
			if (dataProviders == null) throw new ArgumentNullException(nameof(dataProviders));
			if (dataProviders.Count == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(dataProviders));

			return dataProviders
				.SelectMany(Load)
				.ToArray();
		}

		private static IReadOnlyCollection<Entity> Load(IDataProvider dataProvider)
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

		private static InvalidOperationException BuildNoEntitiesException(IDataProvider dataProvider) => 
			new(BuildNoEntitiesErrorMessage(dataProvider));

		private static InvalidOperationException BuildNoEntitiesException(
			IVerboseDataProvider dataProvider,
			VerboseDataProviderResult loadResult)
		{
			return new InvalidOperationException(
				BuildNoEntitiesErrorMessage(dataProvider) +
				BuildOriginsMessage(". Loaded", loadResult.LoadedOrigins) +
				BuildOriginsMessage(". Skipped", loadResult.SkippedOrigins));

			static string BuildOriginsMessage(string sourcesName, IReadOnlyCollection<EntityOrigin> origins) =>
				origins.Count > 0
					? $"{sourcesName} origins are {string.Join(", ", origins.Distinct().Select(s => s.OriginName))}"
					: string.Empty;
		}

		private static string BuildNoEntitiesErrorMessage(IDataProvider dataProvider) =>
			$"One of the specified data providers wasn't able to provide any entities, " +
			"while at least one is expected. " +
			$"Make sure {dataProvider.GetType().Name} configuration and usage is correct " +
			"or simply remove it, if it's not needed.";
	}
}
