using JetBrains.Annotations;
using Reseed.Dsl.Cleanup;
using Reseed.Dsl.Simple;
using Reseed.Dsl.TemporaryTables;

namespace Reseed.Dsl
{
	[PublicAPI]
	public abstract class RenderMode
	{
		public static RenderMode Simple(
			[NotNull] SimpleInsertDefinition insertDefinition,
			[NotNull] CleanupDefinition cleanupDefinition) =>
			new SimpleMode(insertDefinition, cleanupDefinition);

		public static RenderMode TemporaryTables(
			[NotNull] string schemaName,
			[NotNull] TemporaryTablesInsertDefinition insertDefinition,
			[NotNull] CleanupDefinition cleanupDefinition) =>
			new TemporaryTablesMode(schemaName, insertDefinition, cleanupDefinition);
	}
}