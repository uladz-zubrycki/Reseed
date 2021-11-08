using System;
using JetBrains.Annotations;
using Reseed.Configuration.Cleanup;

namespace Reseed.Configuration.Basic
{
	internal sealed class BasicSeedMode : SeedMode
	{
		public readonly BasicInsertDefinition InsertDefinition;
		public readonly CleanupDefinition CleanupDefinition;

		public BasicSeedMode(
			[NotNull] BasicInsertDefinition insertDefinition,
			[NotNull] CleanupDefinition cleanupDefinition)
		{
			this.InsertDefinition = insertDefinition ?? throw new ArgumentNullException(nameof(insertDefinition));
			this.CleanupDefinition = cleanupDefinition ?? throw new ArgumentNullException(nameof(cleanupDefinition));
		}
	}
}