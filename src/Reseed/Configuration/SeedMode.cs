using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Configuration.Basic;
using Reseed.Configuration.Cleanup;
using Reseed.Configuration.TemporaryTables;
using Reseed.Data;

namespace Reseed.Configuration
{
	[PublicAPI]
	public abstract class AnySeedMode
	{

	}

	[PublicAPI]
	public abstract class SeedMode : AnySeedMode
	{
		public readonly IReadOnlyCollection<IDataProvider> DataProviders;

		protected SeedMode([NotNull] IReadOnlyCollection<IDataProvider> dataProviders)
		{
			if (dataProviders == null) throw new ArgumentNullException(nameof(dataProviders));
			if (dataProviders.Count == 0)
			{
				throw new ArgumentException("At least one data provider is required", nameof(dataProviders));
			}

			this.DataProviders = dataProviders;
		}

		public static CleanupOnlySeedMode CleanupOnly(
			[NotNull] CleanupDefinition cleanupDefinition) =>
			new(cleanupDefinition);

		public static SeedMode Basic(
			[NotNull] BasicInsertDefinition insertDefinition,
			[NotNull] CleanupDefinition cleanupDefinition,
			[NotNull] params IDataProvider[] dataProviders) =>
			new BasicSeedMode(insertDefinition, cleanupDefinition, dataProviders);

		public static SeedMode TemporaryTables(
			[NotNull] string schemaName,
			[NotNull] TemporaryTablesInsertDefinition insertDefinition,
			[NotNull] CleanupDefinition cleanupDefinition,
			[NotNull] params IDataProvider[] dataProviders) =>
			new TemporaryTablesSeedMode(
				schemaName, 
				insertDefinition, 
				cleanupDefinition,
				dataProviders);
	}
}