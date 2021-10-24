using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Rendering.Schema
{
	public sealed class TableDefinition : IEquatable<TableDefinition>
	{
		private readonly HashSet<string> columnNames;
		public readonly ObjectName Name;
		public readonly Key PrimaryKey;
		public readonly IReadOnlyCollection<Column> Columns;

		public TableDefinition(
			[NotNull] ObjectName name, 
			[CanBeNull] Key primaryKey,
			IReadOnlyCollection<Column> columns)
		{
			if (columns == null) throw new ArgumentNullException(nameof(columns));
			if (columns.Count == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(columns));
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.PrimaryKey = primaryKey;
			this.Columns = columns;
			this.columnNames = Columns.Select(c => c.Name).ToHashSet();
		}

		public bool HasColumn([NotNull] string columnName)
		{
			if (columnName == null) throw new ArgumentNullException(nameof(columnName));
			return this.columnNames.Contains(columnName);
		}

		public TableDefinition MapTableName([NotNull] Func<ObjectName, ObjectName> mapper)
		{
			if (mapper == null) throw new ArgumentNullException(nameof(mapper));
			return new TableDefinition(mapper(this.Name), this.PrimaryKey, this.Columns);
		}

		public override bool Equals(object obj) => Equals(obj as TableDefinition);

		public bool Equals(TableDefinition other) =>
			other is not null &&
			(ReferenceEquals(other, this) ||
			 Equals(this.Name, other.Name));

		public override int GetHashCode() =>
			539060726 + EqualityComparer<ObjectName>.Default.GetHashCode(this.Name);

		public override string ToString() => this.Name.ToString();
	}
}