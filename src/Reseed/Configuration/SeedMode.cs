using JetBrains.Annotations;
using Reseed.Configuration.Cleanup;
using Reseed.Configuration.Simple;
using Reseed.Configuration.TemporaryTables;

namespace Reseed.Configuration
{
	[PublicAPI]
	public abstract class SeedMode
	{
		public static SeedMode Simple(
			[NotNull] SimpleInsertDefinition insertDefinition,
			[NotNull] CleanupDefinition cleanupDefinition) =>
			new SimpleMode(insertDefinition, cleanupDefinition);

		public static SeedMode TemporaryTables(
			[NotNull] string schemaName,
			[NotNull] TemporaryTablesInsertDefinition insertDefinition,
			[NotNull] CleanupDefinition cleanupDefinition) =>
			new TemporaryTablesMode(schemaName, insertDefinition, cleanupDefinition);
	}
}