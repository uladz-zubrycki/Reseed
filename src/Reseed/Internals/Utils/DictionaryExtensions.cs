using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Reseed.Internals.Utils
{
	internal static class DictionaryExtensions
	{
		public static IReadOnlyCollection<(TKey, TValue)> Pairs<TKey, TValue>(
			[NotNull] this IDictionary<TKey, TValue> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			return source
				.Select(kv => (kv.Key, kv.Value))
				.ToArray();
		}
	}
}
