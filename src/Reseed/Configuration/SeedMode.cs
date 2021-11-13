using System;
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
		public readonly IDataProvider DataProvider;

		protected SeedMode([NotNull] IDataProvider dataProvider)
		{
			this.DataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
		}

		public static CleanupOnlySeedMode CleanupOnly(
			[NotNull] CleanupDefinition cleanupDefinition) =>
			new(cleanupDefinition);

		public static SeedMode Basic(
			[NotNull] BasicInsertDefinition insertDefinition,
			[NotNull] CleanupDefinition cleanupDefinition,
			[NotNull] IDataProvider dataProvider) =>
			new BasicSeedMode(insertDefinition, cleanupDefinition, dataProvider);

		public static SeedMode TemporaryTables(
			[NotNull] string schemaName,
			[NotNull] TemporaryTablesInsertDefinition insertDefinition,
			[NotNull] CleanupDefinition cleanupDefinition,
			[NotNull] IDataProvider dataProvider) =>
			new TemporaryTablesSeedMode(
				schemaName, 
				insertDefinition, 
				cleanupDefinition,
				dataProvider);
	}
}