using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Configuration.Cleanup
{
	[PublicAPI]
	public sealed class CleanupConfiguration
	{
		internal readonly CleanupMode Mode;
		private readonly ICleanupFilter filter;
		private readonly Dictionary<ObjectName, string> customScripts;

		private CleanupConfiguration(
			CleanupMode mode,
			[NotNull] ICleanupFilter filter,
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

		public static CleanupConfiguration IncludeAll(
			CleanupMode mode,
			[CanBeNull] Func<ExcludingCleanupFilter, ExcludingCleanupFilter> configure = null,
			[CanBeNull] IReadOnlyCollection<(ObjectName table, string script)> customScripts = null)
		{
			var configureFilter = configure ?? Fn.Identity<ExcludingCleanupFilter>();
			var filter = configureFilter(new ExcludingCleanupFilter());
			return new CleanupConfiguration(
				mode,
				filter,
				customScripts ?? Array.Empty<(ObjectName table, string script)>());
		}

		public static CleanupConfiguration IncludeNone(
			CleanupMode mode,
			[NotNull] Func<IncludingCleanupFilter, IncludingCleanupFilter> configure,
			[CanBeNull] IReadOnlyCollection<(ObjectName table, string script)> customScripts = null)
		{
			if (configure == null) throw new ArgumentNullException(nameof(configure));
			var filter = configure(new IncludingCleanupFilter());
			return new CleanupConfiguration(
				mode,
				filter,
				customScripts ?? Array.Empty<(ObjectName table, string script)>());
		}

		private static void VerifyCustomScripts(
			IReadOnlyCollection<(ObjectName table, string script)> customScripts,
			ICleanupFilter filter)
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

			var excludedScripts = customScripts
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
}