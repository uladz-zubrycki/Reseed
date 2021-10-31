using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Schema;

namespace Reseed.Internals.Graphs
{
	internal sealed class MutualReference<T> where T : class
	{
		public readonly ReferencePath<T> Left;
		public readonly ReferencePath<T> Right;
		public readonly IReadOnlyCollection<T> Items;
		public readonly IReadOnlyCollection<Relation<T>> Relations;

		public MutualReference([NotNull] ReferencePath<T> left, [NotNull] ReferencePath<T> right)
		{
			if (left == null) throw new ArgumentNullException(nameof(left));
			if (right == null) throw new ArgumentNullException(nameof(right));
			if (left.Target != right.Source)
				throw new ArgumentException(
					$"Left source item differs from the right target one, expected {right.Source}, but got {left.Target}");
			if (right.Target != left.Source)
				throw new ArgumentException(
					$"Right source item differs from the left target one, expected {left.Source}, but got {right.Target}");

			this.Left = left;
			this.Right = right;
			this.Items = left.Items.Concat(right.Items).Distinct().ToArray();
			this.Relations = left.Relations.Concat(right.Relations).Distinct().ToArray();
		}

		public override string ToString() => $"{this.Left.Source} <-> {this.Right.Source}";

		public MutualReference<TOut> Map<TOut>([NotNull] Func<T, TOut> mapper) where TOut : class
		{
			if (mapper == null) throw new ArgumentNullException(nameof(mapper));
			return new MutualReference<TOut>(this.Left.Map(mapper), this.Right.Map(mapper));
		}
	}
}