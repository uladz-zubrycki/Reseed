using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Ordering;
using Reseed.Schema;

namespace Reseed.Generation
{
	[PublicAPI]
	public interface ISeedAction
	{
		public string Name { get; }
		public string ToVerboseString();
	}

	[PublicAPI]
	public sealed class SqlScriptAction : ISeedAction
	{
		public string Name { get; }
		public readonly string Text;

		internal SqlScriptAction([NotNull] string name, [NotNull] string text)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			this.Text = text ?? throw new ArgumentNullException(nameof(text));
		}

		public override string ToString() => $"Script: '{Name}'";

		public string ToVerboseString() => $"'{Name}' script '{Text}'";

		internal SqlScriptAction Map([NotNull] Func<string, string> mapper, string name = null)
		{
			if (mapper == null) throw new ArgumentNullException(nameof(mapper));
			return new SqlScriptAction(name ?? Name, mapper(this.Text));
		}

		internal static SqlScriptAction Join(
			[NotNull] string name,
			[NotNull] IEnumerable<OrderedItem<SqlScriptAction>> scripts)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (scripts == null) throw new ArgumentNullException(nameof(scripts));

			var parts = scripts
				.Order()
				.Select((s, i) =>
					string.Join(
						Environment.NewLine,
						$"-- {i + 1}: {s.Name}",
						s.Text));

			return new SqlScriptAction(
				name,
				string.Join(Environment.NewLine + Environment.NewLine, parts));
		}
	}

	[PublicAPI]
	public sealed class SqlBulkCopyAction : ISeedAction
	{
		public readonly string SourceScript;
		public readonly ObjectName DestinationTable;
		public readonly SqlBulkCopyOptions Options;
		public readonly IReadOnlyCollection<SqlBulkCopyColumnMapping> Columns;

		public string Name { get; }

		internal SqlBulkCopyAction(
			[NotNull] string name,
			[NotNull] string sourceScript,
			[NotNull] ObjectName destinationTable,
			SqlBulkCopyOptions options,
			[NotNull] IReadOnlyCollection<SqlBulkCopyColumnMapping> columns)
		{
			if (columns == null) throw new ArgumentNullException(nameof(columns));
			if (columns.Count == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(columns));

			this.DestinationTable = destinationTable ?? throw new ArgumentNullException(nameof(destinationTable));
			this.SourceScript = sourceScript ?? throw new ArgumentNullException(nameof(sourceScript));
			this.Options = options;
			this.Columns = columns;

			Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		public override string ToString() => $"SqlBulkCopy: '{Name}'";

		public string ToVerboseString() =>
			$"'{Name}' bulk copy into '{DestinationTable}', " +
			$"{SourceScript}";
	}
}