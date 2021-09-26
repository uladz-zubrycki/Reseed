using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Ordering;

namespace Reseed.Schema
{
	internal sealed class Key : IEquatable<Key>
	{
		public readonly IReadOnlyCollection<OrderedItem<string>> Columns;

		public Key([NotNull] IReadOnlyCollection<OrderedItem<string>> columns)
		{
			if (columns == null) throw new ArgumentNullException(nameof(columns));
			if (columns.Count == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(columns));
			if (columns.Any(o => string.IsNullOrEmpty(o.Value)))
				throw new ArgumentException("Column name can't be null or empty string", nameof(columns));
			this.Columns = columns;
		}

		public override bool Equals(object obj) => 
			Equals(obj as Key);

		public bool Equals(Key other) =>
			other != null &&
			this.Columns.OrderBy(o => o.Order)
				.Zip(other.Columns.OrderBy(o => o.Order),
					(l, r) =>
						l.Value.Equals(r.Value, StringComparison.OrdinalIgnoreCase))
				.All(_ => _);

		public override int GetHashCode() =>
			-1952516548 + EqualityComparer<string>.Default.GetHashCode(string.Join(",",
				this.Columns.Order()));

		public override string ToString() => string.Join(", ", this.Columns.Order());
	}
}