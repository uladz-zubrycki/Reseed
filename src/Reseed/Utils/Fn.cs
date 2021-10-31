using System;

namespace Reseed.Utils
{
	internal static class Fn
	{
		public static Func<T, T> Identity<T>() => IdentityImpl<T>.Instance;

		private static class IdentityImpl<T>
		{
			public static readonly Func<T, T> Instance = _ => _;
		}
	}
}
