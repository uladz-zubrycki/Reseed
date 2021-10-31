using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Generation.Schema;
using Reseed.Graphs;
using Testing.Common.Api.Schema;

namespace Reseed.Ordering
{
	internal sealed class TableRow: IMutableNode<TableRow>, IEquatable<TableRow>
	{
		private readonly List<Reference<TableRow>> references;

		public readonly Row Row;
		public readonly TableDefinition Table;
		public IReadOnlyCollection<Reference<TableRow>> References => this.references;
	
		public TableRow(
			[NotNull] Row row, 
			[NotNull] TableDefinition table, 
			[NotNull] IReadOnlyCollection<Reference<TableRow>> references)
		{
			if (references == null) throw new ArgumentNullException(nameof(references));
			this.Table = table ?? throw new ArgumentNullException(nameof(table));
			this.Row = row ?? throw new ArgumentNullException(nameof(row));
			this.references = new List<Reference<TableRow>>(references);
		}

		public void AddReferences([NotNull] IReadOnlyCollection<Reference<TableRow>> items)
		{
			if (items == null) throw new ArgumentNullException(nameof(items));
			this.references.AddRange(items);
		}

		public TableRow With([NotNull] IReadOnlyCollection<Reference<TableRow>> items)
		{
			if (items == null) throw new ArgumentNullException(nameof(items));
			return new TableRow(this.Row, this.Table, items);
		}

		public override string ToString() => this.Row.ToString();

		public override bool Equals(object obj) => Equals(obj as TableRow);

		// todo: consider using PK from TableDefinition for equality
		public bool Equals(TableRow other) =>
			other is not null &&
			(ReferenceEquals(other, this) ||
			 Equals(this.Row, other.Row));

		public override int GetHashCode() => 
			-343017389 + EqualityComparer<Row>.Default.GetHashCode(this.Row);
	}
}