using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Reseed.Tests.Integration.Core
{
	public sealed class SqlEngine
	{
		private readonly string connectionString;

		public SqlEngine(string connectionString)
		{
			this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
		}

		public Task ExecuteNonQueryAsync(string script) =>
			ExecuteAsync(script, c => c.ExecuteNonQueryAsync());

		public Task<T> ExecuteScalarAsync<T>(string script) =>
			ExecuteAsync(script, async c => (T) await c.ExecuteScalarAsync());

		private async Task<T> ExecuteAsync<T>(string script, Func<SqlCommand, Task<T>> execute)
		{
			await using var connection = new SqlConnection(connectionString);
			await using var command = connection.CreateCommand();
			command.CommandText = script;
			await connection.OpenAsync();
			return await execute(command);
		}
	}
}