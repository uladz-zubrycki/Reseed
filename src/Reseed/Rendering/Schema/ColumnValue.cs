using System;
using JetBrains.Annotations;

namespace Reseed.Rendering.Schema
{
	internal sealed class ColumnValue
	{
		public readonly Column Column;
		public readonly string Value;

		public ColumnValue([NotNull] Column column, string value)
		{
			this.Column = column ?? throw new ArgumentNullException(nameof(column));
			this.Value = value;
		}
	}
}
