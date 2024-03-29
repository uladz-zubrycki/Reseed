﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Configuration.Cleanup;
using Reseed.Data;

namespace Reseed.Configuration.TemporaryTables
{
	internal sealed class TemporaryTablesSeedMode : SeedMode
	{
		public readonly string SchemaName;
		public readonly TemporaryTablesInsertDefinition InsertDefinition;
		public readonly CleanupDefinition CleanupDefinition;

		public TemporaryTablesSeedMode(
			[NotNull] string schemaName,
			[NotNull] TemporaryTablesInsertDefinition insertDefinition,
			[NotNull] CleanupDefinition cleanupDefinition,
			[NotNull] IReadOnlyCollection<IDataProvider> dataProviders)
			: base(dataProviders)
		{
			if (string.IsNullOrEmpty(schemaName))
				throw new ArgumentException("Value cannot be null or empty.", nameof(schemaName));

			this.SchemaName = schemaName;
			this.InsertDefinition = insertDefinition ?? throw new ArgumentNullException(nameof(insertDefinition));
			this.CleanupDefinition = cleanupDefinition ?? throw new ArgumentNullException(nameof(cleanupDefinition));
		}
	}
}