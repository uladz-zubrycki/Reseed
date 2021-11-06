using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Data.Providers.FileSystem;

namespace Reseed.Data
{
	internal sealed class EntityLoadResult
	{
		public readonly IReadOnlyCollection<Entity> Entities;
		public readonly IReadOnlyCollection<DataFile> LoadedSources;
		public readonly IReadOnlyCollection<DataFile> SkippedSources;

		public EntityLoadResult(
			[NotNull] IReadOnlyCollection<Entity> entities,
			[NotNull] IReadOnlyCollection<DataFile> sources,
			[NotNull] IReadOnlyCollection<DataFile> skippedSources)
		{
			Entities = entities ?? throw new ArgumentNullException(nameof(entities));
			LoadedSources = sources ?? throw new ArgumentNullException(nameof(sources));
			SkippedSources = skippedSources ?? throw new ArgumentNullException(nameof(skippedSources));
		}
	}

	internal interface IVerboseDataProvider : IDataProvider
	{
		EntityLoadResult GetEntitiesDetailed();
	}
}