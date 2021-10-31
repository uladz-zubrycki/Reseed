using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Ordering
{
	internal sealed class MutualGroup<T>
	{
		private readonly HashSet<T> itemSet;
		public readonly OrderedItem<T>[] Items;
		public readonly IReadOnlyCollection<Relation<T>> Relations;
		public readonly int MinOrder;
		public readonly int MaxOrder;

		public MutualGroup(
			[NotNull] OrderedItem<T>[] items,
			[NotNull] IReadOnlyCollection<Relation<T>> relations)
		{
			if (items == null) throw new ArgumentNullException(nameof(items));
			if (relations == null) throw new ArgumentNullException(nameof(relations));
			if (items.Length == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(items));
			if (relations.Count == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(relations));

			this.Items = items;
			this.Relations = relations;
			this.MinOrder = this.Items.Select(o => o.Order).Min();
			this.MaxOrder = this.Items.Select(o => o.Order).Max();
			this.itemSet = this.Items.Select(o => o.Value).ToHashSet();
		}
		
		public bool Contains(T row) => this.itemSet.Contains(row);
	}
}