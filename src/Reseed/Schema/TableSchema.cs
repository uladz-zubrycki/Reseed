using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Internals.Graphs;
using Testing.Common.Api.Schema;

namespace Reseed.Schema
{
	internal sealed class TableSchema : IMutableNode<TableSchema>
	{
		private readonly List<Reference<TableSchema>> references;

		public readonly ObjectName Name;
		public readonly Key PrimaryKey;
		public readonly IReadOnlyCollection<ColumnSchema> Columns;
		public IReadOnlyCollection<Reference<TableSchema>> References => this.references;
		
		private TableSchema(
			[NotNull] ObjectName name,
			[NotNull] IReadOnlyCollection<ColumnSchema> columns,
			[CanBeNull] Key primaryKey)
		{
			if (columns == null) throw new ArgumentNullException(nameof(columns));
			if (columns.Count == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(columns));
			VerifyPrimaryKeyColumns(columns, primaryKey);

			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.Columns = columns;
			this.PrimaryKey = primaryKey;
		}

		public TableSchema(
			[NotNull] ObjectName name,
			[NotNull] IReadOnlyCollection<ColumnSchema> columns,
			[NotNull] Key primaryKey,
			[NotNull] IReadOnlyCollection<Reference<TableSchema>> references)
			: this(name, columns, primaryKey)
		{
			this.references = new List<Reference<TableSchema>>(references);
		}

		public override string ToString() => this.Name.ToString();

		public void AddReferences(IReadOnlyCollection<Reference<TableSchema>> items)
		{
			if (items == null) throw new ArgumentNullException(nameof(items));
			this.references.AddRange(items);
		}

		public TableSchema With([NotNull] IReadOnlyCollection<Reference<TableSchema>> items)
		{
			if (items == null) throw new ArgumentNullException(nameof(items));
			return new TableSchema(
				this.Name,
				this.Columns,
				this.PrimaryKey,
				items);
		}

		private static void VerifyPrimaryKeyColumns(IReadOnlyCollection<ColumnSchema> columns, Key primaryKey)
		{
			if (primaryKey == null)
			{
				return;
			}

			IEnumerable<string> primaryKeyColumnNames = columns
				.Where(c => c.IsPrimaryKey)
				.Select(c => c.Name)
				.ToArray();

			if (!primaryKey.Columns
				.Select(o => o.Value)
				.SequenceEqual(primaryKeyColumnNames))
			{
				throw new ArgumentException(
					"Primary key definition doesn't match the table columns. " +
					$"Expected to have key of columns {string.Join(", ", primaryKeyColumnNames)}, " +
					$"but got key {primaryKey}");
			}
		}
	}
}