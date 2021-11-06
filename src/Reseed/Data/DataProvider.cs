using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Data.Providers.FileSystem;

namespace Reseed.Data
{
	internal static class DataProvider
	{
		public static IReadOnlyCollection<Entity> Load([NotNull] IDataProvider dataProvider)
		{
			if (dataProvider == null) throw new ArgumentNullException(nameof(dataProvider));

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
			EntityLoadResult loadResult)
		{
			return new InvalidOperationException(
				BuildNoEntitiesErrorMessage(dataProvider) +
				BuildSourcesMessage(". Loaded", loadResult.LoadedSources) +
				BuildSourcesMessage(". Skipped", loadResult.SkippedSources));

			static string BuildSourcesMessage(string sourcesName, IReadOnlyCollection<DataFile> sources) =>
				sources.Count > 0
					? $"{sourcesName} sources are {string.Join(", ", sources.Select(s => s.ToString()))}"
					: string.Empty;
		}

		private static string BuildNoEntitiesErrorMessage(IDataProvider dataProvider) =>
			$"The specified {nameof(IDataProvider)} wasn't able to find any entities, " +
			"while at least one is required. " +
			$"Make sure {dataProvider.GetType().Name} configuration is correct";
	}
}
