using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Rendering
{
	[PublicAPI]
	public sealed class CleanupOptions
	{
		internal readonly CleanupMode Mode;
		private readonly IDataCleanupFilter filter;
		private readonly Dictionary<ObjectName, string> customScripts;

		private CleanupOptions(
			CleanupMode mode,
			[NotNull] IDataCleanupFilter filter,
			[NotNull] IReadOnlyCollection<(ObjectName table, string script)> customScripts)
		{
			if (customScripts == null) throw new ArgumentNullException(nameof(customScripts));
			this.Mode = mode;
			this.filter = filter ?? throw new ArgumentNullException(nameof(filter));

			VerifyCustomScripts(customScripts, filter);
			this.customScripts = customScripts.ToDictionary(cs => cs.table, cs => cs.script);
		}

		internal bool ShouldClean([NotNull] ObjectName table)
		{
			if (table == null) throw new ArgumentNullException(nameof(table));
			return this.filter.ShouldClean(table);
		}

		internal bool GetCustomScript([NotNull] ObjectName table, out string script)
		{
			if (table == null) throw new ArgumentNullException(nameof(table));
			return this.customScripts.TryGetValue(table, out script);
		}

		public static CleanupOptions IncludeAll(
			CleanupMode mode,
			[CanBeNull] Func<ExcludingDataCleanupFilter, ExcludingDataCleanupFilter> configure = null,
			[CanBeNull] IReadOnlyCollection<(ObjectName table, string script)> customScripts = null)
		{
			var configureFilter = configure ?? Fn.Identity<ExcludingDataCleanupFilter>();
			var filter = configureFilter(new ExcludingDataCleanupFilter());
			return new CleanupOptions(
				mode,
				filter,
				customScripts ?? Array.Empty<(ObjectName table, string script)>());
		}

		public static CleanupOptions IncludeNone(
			CleanupMode mode,
			[CanBeNull] Func<IncludingDataCleanupFilter, IncludingDataCleanupFilter> configure = null,
			[CanBeNull] IReadOnlyCollection<(ObjectName table, string script)> customScripts = null)
		{
			var configureFilter = configure ?? Fn.Identity<IncludingDataCleanupFilter>();
			var filter = configureFilter(new IncludingDataCleanupFilter());
			return new CleanupOptions(
				mode,
				filter,
				customScripts ?? Array.Empty<(ObjectName table, string script)>());
		}

		private static void VerifyCustomScripts(
			IReadOnlyCollection<(ObjectName table, string script)> customScripts,
			IDataCleanupFilter filter)
		{
			var duplicatedTables = customScripts
				.GroupBy(t => t.table)
				.Where(gr => gr.Count() > 1)
				.Select(gr => gr.Key)
				.ToArray();

			if (duplicatedTables.Any())
			{
				throw new InvalidOperationException(
					"Multiple custom scripts were provided for tables " +
					$"{duplicatedTables.Select(t => t.Name).JoinStrings(", ")}. " +
					"Please, leave the only script for each table");
			}

			(ObjectName table, string script)[] excludedScripts = customScripts
				.Where(cs => !filter.ShouldClean(cs.table))
				.ToArray();

			if (excludedScripts.Any())
			{
				throw new InvalidOperationException(
					"Custom script was provided for tables, which aren't included for cleanup. " +
					"Please, either include tables or delete scripts. " +
					@$"Tables are {excludedScripts
						.Select(s => s.table.Name)
						.JoinStrings(", ")}");
			}
		}
	}

	internal interface IDataCleanupFilter
	{
		bool ShouldClean(ObjectName table);
	}

	[PublicAPI]
	public sealed class ExcludingDataCleanupFilter : IDataCleanupFilter
	{
		private readonly List<string> excludedSchemas = new List<string>();
		private readonly List<ObjectName> excludedTables = new List<ObjectName>();

		public ExcludingDataCleanupFilter ExcludeSchemas([NotNull] params string[] schemas)
		{
			if (schemas == null) throw new ArgumentNullException(nameof(schemas));
			if (schemas.Length == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(schemas));

			this.excludedSchemas.AddRange(schemas);
			return this;
		}

		public ExcludingDataCleanupFilter ExcludeTables([NotNull] params ObjectName[] tables)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (tables.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(tables));

			this.excludedTables.AddRange(tables);
			return this;
		}

		bool IDataCleanupFilter.ShouldClean(ObjectName table) =>
			!this.excludedSchemas.Contains(table.Schema) &&
			!this.excludedTables.Contains(table);
	}

	[PublicAPI]
	public sealed class IncludingDataCleanupFilter : IDataCleanupFilter
	{
		private readonly List<string> includedSchemas = new List<string>();
		private readonly List<ObjectName> includedTables = new List<ObjectName>();

		public IncludingDataCleanupFilter IncludeSchemas([NotNull] params string[] schemas)
		{
			if (schemas == null) throw new ArgumentNullException(nameof(schemas));
			if (schemas.Length == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(schemas));

			this.includedSchemas.AddRange(schemas);
			return this;
		}

		public IncludingDataCleanupFilter IncludeTables([NotNull] params ObjectName[] tables)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (tables.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(tables));

			this.includedTables.AddRange(tables);
			return this;
		}

		bool IDataCleanupFilter.ShouldClean(ObjectName table) =>
			this.includedSchemas.Contains(table.Schema) ||
			this.includedTables.Contains(table);
	}
}