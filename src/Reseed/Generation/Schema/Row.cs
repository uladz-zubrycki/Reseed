using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Data.FileSystem;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Generation.Schema
{
	public sealed class Row : IEquatable<Row>
	{
		private readonly Dictionary<string, string> valueMapping;
		private readonly object identityValue;
		internal readonly TableDefinition Table;
		internal readonly DataFile Origin;
		public readonly IReadOnlyCollection<string> Columns;

		internal Row(
			[NotNull] TableDefinition tableDefinition,
			[NotNull] DataFile origin,
			[NotNull] IReadOnlyCollection<(string column, string value)> values)
		{
			if (values == null) throw new ArgumentNullException(nameof(values));
			if (values.Count == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(values));
			if (values.Any(x => x.value == null))
				throw new ArgumentException("Column value can't be null", nameof(values));

			this.Table = tableDefinition ?? throw new ArgumentNullException(nameof(tableDefinition));
			this.Origin = origin ?? throw new ArgumentNullException(nameof(origin));
			this.valueMapping = values.ToDictionary(v => v.column, v => v.value);
			this.Columns = values.Select(c => c.column).ToHashSet();
			this.identityValue = GetIdentityValue(tableDefinition.PrimaryKey);
		}

		public string GetValue([NotNull] string columnName)
		{
			if (columnName == null) throw new ArgumentNullException(nameof(columnName));
			return this.valueMapping.TryGetValue(columnName, out var value)
				? value
				: null;
		}

		public Row WithValue([NotNull] string columnName, [NotNull] string value)
		{
			if (columnName == null) throw new ArgumentNullException(nameof(columnName));
			if (value == null) throw new ArgumentNullException(nameof(value));

			if (!this.Table.HasColumn(columnName))
			{
				throw new InvalidOperationException(
					$"Can't set column value to '{value}', " +
					$"table '{Table.Name}' doesn't have column with name '{columnName}'");
			}

			var values = valueMapping
				.Where(kv => !string.Equals(kv.Key, columnName, StringComparison.OrdinalIgnoreCase))
				.Select(kv => (kv.Key, kv.Value))
				.Append((columnName, value))
				.ToArray();

			return new Row(Table, Origin, values);
		}

		internal KeyValue GetValue([NotNull] Key key)
		{
			if (key == null) throw new ArgumentNullException(nameof(key));
			return new KeyValue(key,
				key.Columns
					.Select(o => o.Map(GetValue))
					.ToArray());
		}

		public Row MapTableName([NotNull] Func<ObjectName, ObjectName> mapper)
		{
			if (mapper == null) throw new ArgumentNullException(nameof(mapper));
			return new Row(
				this.Table.MapTableName(mapper), 
				this.Origin, 
				this.valueMapping.Pairs());
		}

		public override string ToString() => $"{this.Table}: {this.identityValue}";

		private object GetIdentityValue(Key key)
		{
			if (key != null)
			{
				var keyValue = GetValue(key);
				if (keyValue.HasValue)
				{
					return keyValue;
				}
			}

			return string.Join(";", this
				.Columns
				.OrderBy(_ => _)
				.Select(c => $"{c}={GetValue(c)}"));
		}

		public override bool Equals(object obj) =>
			Equals(obj as Row);

		public bool Equals(Row other) =>
			other is not null &&
			(ReferenceEquals(other, this) ||
			 Equals(this.Table, other.Table) &&
			 Equals(this.identityValue, other.identityValue));

		public override int GetHashCode()
		{
			var hashCode = -1422622732;
			hashCode = hashCode * -1521134295 + this.Table.GetHashCode();
			hashCode = hashCode * -1521134295 + this.identityValue.GetHashCode();
			return hashCode;
		}
	}
}