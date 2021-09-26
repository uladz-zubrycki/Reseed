using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Reseed.Samples.NUnit
{
	// TestFixture containing a few tests, which are using database to perform
	// possibly destructing actions like UPDATE or DELETE to demonstrate that
	// even though there is the only database, tests are isolated from each other
	// and will start operating on the same state.
	// I.e tests won't ever fail, because some other test mutated the state.
	//
	// Note that tests shouldn't run in parallel, there are no guarantees otherwise.
	[Parallelizable(ParallelScope.None)]
	public sealed class SampleFixture: TestFixtureBase
	{
		private const int UsersCount = 3;

		[Test]
		[Repeat(5)]
		public async Task ShouldDeleteUsers()
		{
			// We don't need to do any User table initialization manually.
			// Seeder, invoked in the TestFixtureBase type, will take care of that.
			Assert.AreEqual(UsersCount, await GetUsersCount());

			// Executing destructing action, which won't be noticed in the rest of the tests.
			await DeleteUsers();
			Assert.AreEqual(0, await GetUsersCount());
		}

		[Test]
		[Repeat(5)]
		public async Task ShouldInsertUsers()
		{
			// In spite of the previous test, which might have deleted all the data from User table,
			// we have the expected rows count here.
			Assert.AreEqual(UsersCount, await GetUsersCount());

			// Executing destructing action, which won't be noticed in the rest of the tests.
			await ExecuteNonQueryAsync("INSERT INTO [dbo].[User] VALUES ('Test', 'Test')");
			Assert.AreEqual(UsersCount + 1, await GetUsersCount());
		}

		private static Task DeleteUsers() => 
			ExecuteNonQueryAsync("DELETE FROM [dbo].[User]");

		private static Task<int> GetUsersCount() => 
			ExecuteScalarAsync<int>("SELECT COUNT(1) FROM [dbo].[User]");

		private static Task ExecuteNonQueryAsync(string script) =>
			ExecuteAsync(script, c => c.ExecuteNonQueryAsync());

		private static Task<T> ExecuteScalarAsync<T>(string script) =>
			ExecuteAsync(script, async c => (T) await c.ExecuteScalarAsync());

		private static async Task<T> ExecuteAsync<T>(string script, Func<SqlCommand, Task<T>> execute)
		{
			await using var connection = new SqlConnection(ConnectionString);
			await using var command = connection.CreateCommand();
			command.CommandText = script;
			await connection.OpenAsync();
			return await execute(command);
		}
	}
}
