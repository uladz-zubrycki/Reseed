using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Configuration.Cleanup
{
	[PublicAPI]
	public sealed class CleanupOptions
	{
		internal readonly CleanupKind Kind;
		private readonly IDataCleanupFilter filter;
		private readonly Dictionary<ObjectName, string> customScripts;

		private CleanupOptions(
			CleanupKind kind,
			[NotNull] IDataCleanupFilter filter,
			[NotNull] IReadOnlyCollection<(ObjectName table, string script)> customScripts)
		{
			if (customScripts == null) throw new ArgumentNullException(nameof(customScripts));
			this.Kind = kind;
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
			CleanupKind kind,
			[CanBeNull] Func<ExcludingDataCleanupFilter, ExcludingDataCleanupFilter> configure = null,
			[CanBeNull] IReadOnlyCollection<(ObjectName table, string script)> customScripts = null)
		{
			var configureFilter = configure ?? Fn.Identity<ExcludingDataCleanupFilter>();
			var filter = configureFilter(new ExcludingDataCleanupFilter());
			return new CleanupOptions(
				kind,
				filter,
				customScripts ?? Array.Empty<(ObjectName table, string script)>());
		}

		public static CleanupOptions IncludeNone(
			CleanupKind kind,
			[NotNull] Func<IncludingDataCleanupFilter, IncludingDataCleanupFilter> configure,
			[CanBeNull] IReadOnlyCollection<(ObjectName table, string script)> customScripts = null)
		{
			if (configure == null) throw new ArgumentNullException(nameof(configure));
			var filter = configure(new IncludingDataCleanupFilter());
			return new CleanupOptions(
				kind,
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