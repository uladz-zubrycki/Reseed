using System;
using System.Linq;
using JetBrains.Annotations;

namespace Reseed.Schema
{
	internal sealed class ColumnSchema
	{
		public readonly int Order;
		public readonly string Name;
		public readonly DataType DataType;
		public readonly bool IsPrimaryKey;
		public readonly bool IsIdentity;
		public readonly bool IsComputed;
		public readonly bool IsNullable;
		public readonly string DefaultValueExpression;
		public readonly bool HasDefaultValue;

		public ColumnSchema(
			int order,
			[NotNull] string name,
			[NotNull] DataType dataType,
			bool isPrimaryKey,
			bool isIdentity,
			bool isComputed,
			bool isNullable,
			string defaultValueExpression)
		{
			if (order < 0) throw new ArgumentOutOfRangeException(nameof(order));
			this.Order = order;
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
			this.IsPrimaryKey = isPrimaryKey;
			this.IsIdentity = isIdentity;
			this.IsComputed = isComputed;
			this.IsNullable = isNullable;
			this.HasDefaultValue = defaultValueExpression != null;
			this.DefaultValueExpression = defaultValueExpression;
		}

		public override string ToString()
		{
			var options =
				new[]
					{
						this.IsPrimaryKey ? "PK" : string.Empty,
						this.IsIdentity ? "Identity" : string.Empty,
						this.HasDefaultValue ? this.DefaultValueExpression : string.Empty
					}
					.Where(s => !string.IsNullOrEmpty(s));

			return string.Join(", ", new[]
				{
					$"{this.Name} ({this.DataType}, {(this.IsNullable ? "null" : "not null")})",
					string.Join(", ", options)
				}
				.Where(s => !string.IsNullOrEmpty(s)));
		}
	}
}