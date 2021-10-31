using System;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Internals.Utils;
using Reseed.Ordering;

namespace Reseed.Schema
{
	internal sealed class KeyValue : IEquatable<KeyValue>
	{
		private readonly string value;
		private readonly Key key;
		public readonly OrderedItem<string>[] Values;

		public bool HasValue;

		public KeyValue([NotNull] Key key, [NotNull] OrderedItem<string>[] values)
		{
			if (values == null) throw new ArgumentNullException(nameof(values));
			if (values.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(values));

			this.key = key ?? throw new ArgumentNullException(nameof(key));
			this.Values = values;
			this.value = values.Order().JoinStrings(";");
			this.HasValue = values.All(v => v.Value != null);
		}

		public override bool Equals(object obj) =>
			Equals(obj as KeyValue);

		public bool Equals(KeyValue other) =>
			other is not null &&
			(ReferenceEquals(this, other) || Equals(this.value, other.value));

		public override int GetHashCode() => this.value.GetHashCode();

		public override string ToString() =>
			this.key.Columns.Order()
				.Zip(this.Values.Order(),
					(n, v) => $"{n} = {v}")
				.JoinStrings(", ");
	}
}