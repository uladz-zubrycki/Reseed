using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Configuration.Cleanup;
using Reseed.Data;

namespace Reseed.Configuration.Basic
{
	internal sealed class BasicSeedMode : SeedMode
	{
		public readonly BasicInsertDefinition InsertDefinition;
		public readonly CleanupDefinition CleanupDefinition;

		public BasicSeedMode(
			[NotNull] BasicInsertDefinition insertDefinition,
			[NotNull] CleanupDefinition cleanupDefinition,
			[NotNull] IReadOnlyCollection<IDataProvider> dataProviders)
			: base(dataProviders)
		{
			this.InsertDefinition = insertDefinition ?? throw new ArgumentNullException(nameof(insertDefinition));
			this.CleanupDefinition = cleanupDefinition ?? throw new ArgumentNullException(nameof(cleanupDefinition));
		}
	}
}