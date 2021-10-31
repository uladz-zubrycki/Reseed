using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Reseed.Internals.Utils
{
	internal sealed class InlineEqualityComparer<T, TResult> : EqualityComparer<T>
	{
		private readonly Func<T, TResult> map;

		public InlineEqualityComparer([NotNull] Func<T, TResult> compareBy)
		{
			this.map = compareBy ?? throw new ArgumentNullException(nameof(compareBy));
		}

		public override bool Equals(T x, T y) =>
			x is null && y is null ||
			x is not null && y is not null &&
			(ReferenceEquals(x, y) ||
			 Equals(this.map(x), this.map(y)));

		public override int GetHashCode(T obj) => 
			this.map(obj).GetHashCode();
	}

	internal static class InlineEqualityComparer<T>
	{
		public static InlineEqualityComparer<T, TResult> Create<TResult>(Func<T, TResult> compareBy) =>
			new(compareBy);
	}
}
