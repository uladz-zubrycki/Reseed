using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Reseed.Data.Providers
{
	internal interface IVerboseDataProvider : IDataProvider
	{
		VerboseDataProviderResult GetEntitiesDetailed();
	}

	internal sealed class VerboseDataProviderResult
	{
		public readonly IReadOnlyCollection<Entity> Entities;
		public readonly IReadOnlyCollection<EntityOrigin> LoadedOrigins;
		public readonly IReadOnlyCollection<EntityOrigin> SkippedOrigins;

		public VerboseDataProviderResult(
			[NotNull] IReadOnlyCollection<Entity> entities,
			[NotNull] IReadOnlyCollection<EntityOrigin> loadedOrigins,
			[NotNull] IReadOnlyCollection<EntityOrigin> skippedOrigins)
		{
			Entities = entities ?? throw new ArgumentNullException(nameof(entities));
			LoadedOrigins = loadedOrigins ?? throw new ArgumentNullException(nameof(loadedOrigins));
			SkippedOrigins = skippedOrigins ?? throw new ArgumentNullException(nameof(skippedOrigins));
		}
	}
}