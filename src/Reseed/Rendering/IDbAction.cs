using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Schema;

namespace Reseed.Rendering
{
	[PublicAPI]
	public interface IDbAction
	{
		public string Name { get; }
	}

	[PublicAPI]
	public sealed class DbScript: IDbAction
	{
		public string Name {get; }
		public readonly string Text;

		internal DbScript([NotNull] string name, [NotNull] string text)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			this.Text = text ?? throw new ArgumentNullException(nameof(text));
		}

		public override string ToString() => $"Script: {Name}";

		internal DbScript Map([NotNull] Func<string, string> mapper, string name = null)
		{
			if (mapper == null) throw new ArgumentNullException(nameof(mapper));
			return new DbScript(name ?? Name, mapper(this.Text));
		}

		internal static DbScript Join([NotNull] string name, [NotNull] IEnumerable<DbScript> scripts)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (scripts == null) throw new ArgumentNullException(nameof(scripts));

			return new DbScript(name,
				string.Join(
					Environment.NewLine + Environment.NewLine,
					scripts.Select((s, i) =>
						string.Join(Environment.NewLine,
							$"-- {i + 1}: {s.Name}",
							s.Text))));
		}
	}

	[PublicAPI]
	public sealed class SqlBulkCopyAction : IDbAction
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

		public override string ToString() => $"SqlBulkCopy: {Name}";
	}
}