using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Reseed.Ordering
{
	[PublicAPI]
	public sealed class OrderedItem<T> 
	{
		public readonly int Order;
		public readonly T Value;

		public OrderedItem(int order, T value)
		{
			this.Order = order;
			this.Value = value;
		}

		public override string ToString() => 
			$"{this.Order}: {this.Value}";

		internal OrderedItem<TResult> Map<TResult>([NotNull] Func<T, TResult> mapper)
		{
			if (mapper == null) throw new ArgumentNullException(nameof(mapper));
			return new OrderedItem<TResult>(this.Order, mapper(this.Value));
		}

		internal OrderedItem<T> MapOrder([NotNull] Func<int, int> mapper)
		{
			if (mapper == null) throw new ArgumentNullException(nameof(mapper));
			return new OrderedItem<T>(mapper(this.Order), this.Value);
		}
	}

	internal static class OrderedItem
	{
		public static OrderedItem<T> Ordered<T>(int order, T value) =>
			new(order, value);

		public static IReadOnlyCollection<OrderedItem<T>> OrderedCollection<T>([NotNull] params T[] items)
		{
			if (items == null) throw new ArgumentNullException(nameof(items));
			return items.WithNaturalOrder().ToArray();
		}

		public static IReadOnlyCollection<OrderedItem<T>> Flatten<T>([NotNull] this OrderedItem<OrderedItem<T>[]>[] source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			return Enumerate().ToArray();

			IEnumerable<OrderedItem<T>> Enumerate()
			{
				var i = 0;
				foreach (var groupItem in source.OrderBy(o => o.Order))
				{
					foreach (var item in groupItem.Value.OrderBy(o => o.Order))
					{
						yield return item.MapOrder(_ => i);
						i++;
					}
				}
			}
		}

		public static IEnumerable<T> Order<T>([NotNull] this IEnumerable<OrderedItem<T>> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			return source.OrderBy(o => o.Order).Select(o => o.Value);
		}

		public static IEnumerable<T> Unordered<T>([NotNull] this IEnumerable<OrderedItem<T>> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			return source.Select(o => o.Value);
		}

		public static IEnumerable<OrderedItem<T>> WithNaturalOrder<T>([NotNull] this IEnumerable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			return source.Select((item, i) => Ordered(i, item));
		}

		public static IReadOnlyCollection<OrderedItem<T>> Concat<T>(
			[NotNull] this IReadOnlyCollection<OrderedItem<T>> first,
			[NotNull] IReadOnlyCollection<OrderedItem<T>> second)
		{
			if (first == null) throw new ArgumentNullException(nameof(first));
			if (second == null) throw new ArgumentNullException(nameof(second));

			return first
				.Order()
				.Concat(second.Order())
				.WithNaturalOrder()
				.ToArray();
		}

		public static IReadOnlyCollection<OrderedItem<T>> Append<T>(
			this IReadOnlyCollection<OrderedItem<T>> items,
			T item)
		{
			var lastIndex = items.Max(i => i.Order);
			return items
				.Append(new OrderedItem<T>(lastIndex + 1, item))
				.ToArray();
		}
	}
}