using System;
using JetBrains.Annotations;

namespace Reseed.Rendering
{
	internal sealed class Column
	{
		public readonly int Order;
		public readonly string Name;
		public readonly bool HasQuotedLiteral;
		public readonly bool IsRequired;
		public readonly string DefaultValue;
		public readonly bool IsIdentity;
		public readonly bool IsPrimaryKey;

		public Column(
			int order,
			[NotNull] string name,
			bool hasQuotedLiteral,
			bool isRequired,
			bool isIdentity,
			bool isPrimaryKey,
			[CanBeNull] string defaultValue)
		{
			if (order < 0) throw new ArgumentOutOfRangeException(nameof(order));
			this.Order = order;
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.HasQuotedLiteral = hasQuotedLiteral;
			this.IsRequired = isRequired;
			this.IsIdentity = isIdentity;
			this.IsPrimaryKey = isPrimaryKey;
			this.DefaultValue = defaultValue;
		}

		public override string ToString() => this.Name;
	}
}