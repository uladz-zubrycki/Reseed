using System;
using JetBrains.Annotations;
using Reseed.Configuration.Cleanup;

namespace Reseed.Configuration.Simple
{
	internal sealed class SimpleMode : SeedMode
	{
		public readonly SimpleInsertDefinition InsertDefinition;
		public readonly CleanupDefinition CleanupDefinition;

		public SimpleMode(
			[NotNull] SimpleInsertDefinition insertDefinition,
			[NotNull] CleanupDefinition cleanupDefinition)
		{
			this.InsertDefinition = insertDefinition ?? throw new ArgumentNullException(nameof(insertDefinition));
			this.CleanupDefinition = cleanupDefinition ?? throw new ArgumentNullException(nameof(cleanupDefinition));
		}
	}
}