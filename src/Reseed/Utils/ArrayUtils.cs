using System;
using System.Linq;

namespace Reseed.Utils
{
	internal static class ArrayUtils
	{
		public static T[] Init<T>(int count, Func<int, T> create) =>
			Enumerable.Range(0, count)
				.Select(create)
				.ToArray();
	}
}