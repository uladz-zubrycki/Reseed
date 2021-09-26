using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Reseed.Utils
{
	internal static class EnumerableExtensions
	{
		public static (T[] passed, T[] failed) PartitionBy<T>(
			[NotNull] this IEnumerable<T> source, [NotNull] Func<T, bool> predicate)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));

			var passed = new List<T>();
			var failed = new List<T>();

			foreach (T item in source)
			{
				(predicate(item) ? passed : failed).Add(item);
			}

			return (passed.ToArray(), failed.ToArray());
		}

		public static HashSet<T> ToHashSet<T>([NotNull] this IEnumerable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			return new HashSet<T>(source);
		}

		public static HashSet<T> ToHashSet<T>(
			[NotNull] this IEnumerable<T> source,
			[NotNull] IEqualityComparer<T> comparer)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (comparer == null) throw new ArgumentNullException(nameof(comparer));
			return new HashSet<T>(source, comparer);
		}

		public static string JoinStrings([NotNull] this IEnumerable<string> source, [NotNull] string separator)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (separator == null) throw new ArgumentNullException(nameof(separator));
			return string.Join(separator, source);
		}
	}
}
