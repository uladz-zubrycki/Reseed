using System;
using System.Data.SqlClient;

namespace Reseed.Internals.Utils
{
	internal static class SqlDataReaderExtensions
	{
		public static T TryGet<T>(
			this SqlDataReader reader,
			int columnOrdinal,
			Func<int, T> read)
			where T : class =>
			reader.IsDBNull(columnOrdinal) ? null : read(columnOrdinal);

		public static T? TryGetValue<T>(
			this SqlDataReader reader,
			int columnOrdinal,
			Func<int, T> read)
			where T : struct =>
			reader.IsDBNull(columnOrdinal) ? (T?) null : read(columnOrdinal);
	}
}