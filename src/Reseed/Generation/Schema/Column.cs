using System;
using JetBrains.Annotations;
using Reseed.Schema;

namespace Reseed.Generation.Schema
{
	public sealed class Column
	{
		public readonly int Order;
		public readonly string Name;
		public readonly DataType DataType;
		public readonly bool HasQuotedLiteral;
		public readonly bool IsRequired;
		public readonly string DefaultValue;
		public readonly bool IsIdentity;
		public readonly bool IsPrimaryKey;
		public readonly bool IsComputed;

		public Column(
			int order,
			[NotNull] string name,
			[NotNull] DataType dataType,
			bool hasQuotedLiteral,
			bool isRequired,
			bool isIdentity,
			bool isPrimaryKey,
			bool isComputed,
			[CanBeNull] string defaultValue)
		{
			if (order < 0) throw new ArgumentOutOfRangeException(nameof(order));
			this.Order = order;
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
			this.HasQuotedLiteral = hasQuotedLiteral;
			this.IsRequired = isRequired;
			this.IsIdentity = isIdentity;
			this.IsPrimaryKey = isPrimaryKey;
			this.IsComputed = isComputed;
			this.DefaultValue = defaultValue;
		}

		public override string ToString() => this.Name;
	}
}