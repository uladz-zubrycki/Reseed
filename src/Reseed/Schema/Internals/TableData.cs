using System;
using JetBrains.Annotations;

namespace Reseed.Schema.Internals
{
	internal sealed class TableData
	{
		public readonly ObjectName Name;
		public readonly int ObjectId;
		public readonly Key PrimaryKey;
		public readonly ColumnSchema[] Columns;

		public TableData(
			[NotNull] ObjectName name, 
			int objectId,
			[CanBeNull] Key primaryKey,
			[NotNull] ColumnSchema[] columns)
		{
			if (columns == null) throw new ArgumentNullException(nameof(columns));
			if (columns.Length == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(columns));

			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.ObjectId = objectId;
			this.PrimaryKey = primaryKey;
			this.Columns = columns;
		}

		public override string ToString() => this.Name.ToString();
	}
}