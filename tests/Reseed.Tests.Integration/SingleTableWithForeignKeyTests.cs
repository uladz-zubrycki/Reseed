using System.Threading.Tasks;
using NUnit.Framework;
using Reseed.Tests.Integration.Core;

namespace Reseed.Tests.Integration
{
	[Parallelizable(ParallelScope.All)]
	public sealed class SingleTableWithForeignKeyTests: TestFixtureBase
	{
		[Test]
		// Should insert single table data, when there are entities
		// none of the rows has FK set.
		// No ordering is needed, so could be any.
		public Task ShouldInsert_ForeignKeyMissing() => 
			TestUserTableSeed(4);

		[Test]
		// Should insert single table data, when there are entities
		// every row has FK set, therefore there is a cycle
		// FK needs to be disabled for the graph with cycle,
		// rest of the rows should be ordered by their FK values.
		public Task ShouldInsert_ForeignKeyEvery() => 
			TestUserTableSeed(5);

		[Test]
		// Should insert single table data, when there are entities
		// some of the rows has FK set.
		// Rows should be ordered by their FK values.
		public Task ShouldInsert_ForeignKeyMixed() => 
			TestUserTableSeed(4);

		[Test]
		// Should not break row ordering, while grouping rows by columns, which is done
		// to avoid column names repetition in the script and provide rows
		// with same columns in the only VALUES clause. See InsertScriptRenderer.GroupRowsByColumns
		public Task ShouldPreserveRowOrderWhileGroupingRowsByColumns() => 
			TestUserTableSeed(5);

		private async Task TestUserTableSeed(int userCount)
		{
			await Conventional.AssertSeedSucceeds(
				this,
				RenderModes.SimpleScriptPreferTruncate,
				async sql => Assert.AreEqual(userCount, await GetUsersCount(sql)),
				async sql => Assert.AreEqual(0, await GetUsersCount(sql)));

			static Task<int> GetUsersCount(SqlEngine sqlEngine) =>
				sqlEngine.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM [dbo].[User]");
		}
	}
}
