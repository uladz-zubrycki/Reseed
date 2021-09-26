using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Ordering;
using Reseed.Schema;

namespace Reseed
{
	public sealed class DbActions
	{
		public readonly IReadOnlyCollection<OrderedItem<IDbAction>> PrepareDatabase;
		public readonly IReadOnlyCollection<OrderedItem<IDbAction>> InsertData;
		public readonly IReadOnlyCollection<OrderedItem<IDbAction>> DeleteData;
		public readonly IReadOnlyCollection<OrderedItem<IDbAction>> CleanupDatabase;

		public DbActions(
			[NotNull] IReadOnlyCollection<OrderedItem<IDbAction>> prepareDatabase,
			[NotNull] IReadOnlyCollection<OrderedItem<IDbAction>> insertData,
			[NotNull] IReadOnlyCollection<OrderedItem<IDbAction>> deleteData,
			[NotNull] IReadOnlyCollection<OrderedItem<IDbAction>> cleanupDatabase)
		{
			this.PrepareDatabase = prepareDatabase ?? throw new ArgumentNullException(nameof(prepareDatabase));
			this.InsertData = insertData ?? throw new ArgumentNullException(nameof(insertData));
			this.DeleteData = deleteData ?? throw new ArgumentNullException(nameof(deleteData));
			this.CleanupDatabase = cleanupDatabase ?? throw new ArgumentNullException(nameof(cleanupDatabase));
		}
	}

	public interface IDbAction
	{
		public string Name { get; }
	}

	public sealed class DbScript: IDbAction
	{
		public string Name {get; }
		public readonly string Text;

		public DbScript([NotNull] string name, [NotNull] string text)
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

	public sealed class SqlBulkCopyAction : IDbAction
	{
		public readonly string SourceScript;
		public readonly ObjectName DestinationTable;
		public readonly SqlBulkCopyOptions Options;
		public readonly IReadOnlyCollection<SqlBulkCopyColumnMapping> Columns;

		public string Name { get; }

		public SqlBulkCopyAction(
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

	internal enum DbActionStage
	{
		PrepareDb,
		Insert,
		Delete,
		CleanupDb
	}

	internal sealed class DbStep
	{
		public readonly DbActionStage Stage;
		public readonly IReadOnlyCollection<OrderedItem<IDbAction>> Actions;

		public DbStep(
			DbActionStage stage, 
			[NotNull] IReadOnlyCollection<OrderedItem<IDbAction>> actions)
		{
			this.Stage = stage;
			this.Actions = actions ?? throw new ArgumentNullException(nameof(actions));
		}
	}
}