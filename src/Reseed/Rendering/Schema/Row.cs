using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Data;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Rendering.Schema
{
	internal sealed class Row : IEquatable<Row>
	{
		private readonly Dictionary<string, string> valueMapping;
		private readonly object identityValue;
		private readonly Key primaryKey;
		public readonly ObjectName TableName;
		public readonly IReadOnlyCollection<string> Columns;
		public readonly DataFile Origin;

		public Row(
			[NotNull] ObjectName tableName,
			[CanBeNull] Key primaryKey,
			[NotNull] DataFile origin,
			[NotNull] IReadOnlyCollection<(string column, string value)> values)
		{
			if (values == null) throw new ArgumentNullException(nameof(values));
			if (values.Count == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(values));
			if (values.Any(x => x.value == null))
				throw new ArgumentException("Column value can't be null", nameof(values));

			this.TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
			this.primaryKey = primaryKey;
			this.Origin = origin ?? throw new ArgumentNullException(nameof(origin));
			this.valueMapping = values.ToDictionary(v => v.column, v => v.value);
			this.Columns = values.Select(v => v.column).ToArray();
			this.identityValue = GetIdentityValue(primaryKey);
		}

		public string GetValue([NotNull] string columnName)
		{
			if (columnName == null) throw new ArgumentNullException(nameof(columnName));
			return this.valueMapping.TryGetValue(columnName, out var value)
				? value
				: null;
		}

		public KeyValue GetValue([NotNull] Key key)
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
				mapper(this.TableName), 
				this.primaryKey,
				this.Origin, 
				this.valueMapping.Pairs());
		}

		public override string ToString() => $"{this.TableName}: {this.identityValue}";

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
			 Equals(this.TableName, other.TableName) &&
			 Equals(this.identityValue, other.identityValue));

		public override int GetHashCode()
		{
			var hashCode = -1422622732;
			hashCode = hashCode * -1521134295 + this.TableName.GetHashCode();
			hashCode = hashCode * -1521134295 + this.identityValue.GetHashCode();
			return hashCode;
		}
	}
}