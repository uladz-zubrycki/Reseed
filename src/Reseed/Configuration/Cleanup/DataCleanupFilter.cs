﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Schema;

namespace Reseed.Configuration.Cleanup
{
	internal interface ICleanupFilter
	{
		bool ShouldClean(ObjectName table);
	}

	[PublicAPI]
	public sealed class ExcludingCleanupFilter : ICleanupFilter
	{
		private readonly List<string> excludedSchemas = new();
		private readonly List<ObjectName> excludedTables = new();

		public ExcludingCleanupFilter ExcludeSchemas([NotNull] params string[] schemas)
		{
			if (schemas == null) throw new ArgumentNullException(nameof(schemas));
			if (schemas.Length == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(schemas));

			this.excludedSchemas.AddRange(schemas);
			return this;
		}

		public ExcludingCleanupFilter ExcludeTables([NotNull] params ObjectName[] tables)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (tables.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(tables));

			this.excludedTables.AddRange(tables);
			return this;
		}

		bool ICleanupFilter.ShouldClean(ObjectName table) =>
			!this.excludedSchemas.Contains(table.Schema) &&
			!this.excludedTables.Contains(table);
	}

	[PublicAPI]
	public sealed class IncludingCleanupFilter : ICleanupFilter
	{
		private readonly List<string> includedSchemas = new();
		private readonly List<ObjectName> includedTables = new();

		public IncludingCleanupFilter IncludeSchemas([NotNull] params string[] schemas)
		{
			if (schemas == null) throw new ArgumentNullException(nameof(schemas));
			if (schemas.Length == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(schemas));

			this.includedSchemas.AddRange(schemas);
			return this;
		}

		public IncludingCleanupFilter IncludeTables([NotNull] params ObjectName[] tables)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (tables.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(tables));

			this.includedTables.AddRange(tables);
			return this;
		}

		bool ICleanupFilter.ShouldClean(ObjectName table) =>
			this.includedSchemas.Contains(table.Schema) ||
			this.includedTables.Contains(table);
	}
}