using JetBrains.Annotations;
using Reseed.Configuration.Basic;
using Reseed.Configuration.Cleanup;
using Reseed.Configuration.TemporaryTables;

namespace Reseed.Configuration
{
	[PublicAPI]
	public abstract class SeedMode
	{
		public static SeedMode Basic(
			[NotNull] BasicInsertDefinition insertDefinition,
			[NotNull] CleanupDefinition cleanupDefinition) =>
			new BasicSeedMode(insertDefinition, cleanupDefinition);

		public static SeedMode TemporaryTables(
			[NotNull] string schemaName,
			[NotNull] TemporaryTablesInsertDefinition insertDefinition,
			[NotNull] CleanupDefinition cleanupDefinition) =>
			new TemporaryTablesSeedMode(schemaName, insertDefinition, cleanupDefinition);
	}
}