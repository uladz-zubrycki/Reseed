using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Reseed.Configuration;
using Reseed.Configuration.Cleanup;
using Reseed.Generation;
using Reseed.Schema;
using Reseed.Tests.Integration.Core;

namespace Reseed.Tests.Integration
{
	[Parallelizable(ParallelScope.Fixtures)]
	public sealed class CleanupTests : TestFixtureBase
	{
		[Test]
		public async Task ShouldHandleTableReferencedByIndexedView()
		{
			await using var database = await Conventional.CreateConventionalDatabase(this);
			var sql = new SqlEngine(database.ConnectionString);

			var reseeder = new Reseeder();
			var exception = Assert.Throws<InvalidOperationException>(() => reseeder.Generate(
				database.ConnectionString,
				new CleanupOnlySeedMode(CleanupDefinition.Script(
					CleanupMode.Truncate(),
					CleanupTarget.Excluding()))));
			Assert.That(exception.Message, Is.EqualTo(
				"Cleanup mode 'Truncate' can't truncate tables referenced by indexed views: dbo.User. " +
				"Add these tables to the useDeleteForTables argument or use CleanupMode.PreferTruncate()."));

			var truncateActions = reseeder.Generate(
				database.ConnectionString,
				new CleanupOnlySeedMode(CleanupDefinition.Script(
					CleanupMode.Truncate(new[] { new ObjectName("User") }),
					CleanupTarget.Excluding())));
			Assert.That(
				GetCleanupScript(truncateActions),
				Does.Contain("DELETE FROM [dbo].[User];"));

			var actions = reseeder.Generate(
				database.ConnectionString,
				new CleanupOnlySeedMode(CleanupDefinition.Script(
					CleanupMode.PreferTruncate(),
					CleanupTarget.Excluding())));

			var cleanupScript = GetCleanupScript(actions);

			Assert.That(cleanupScript, Does.Contain("DELETE FROM [dbo].[User];"));
			Assert.That(cleanupScript, Does.Not.Contain("TRUNCATE TABLE [dbo].[User];"));
			reseeder.Execute(database.ConnectionString, actions.RestoreData);
			reseeder.Execute(database.ConnectionString, actions.RestoreData);

			Assert.That(
				await sql.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM [dbo].[User]"),
				Is.Zero);
		}

		private static string GetCleanupScript(SeedActions actions) =>
			string.Join(
				System.Environment.NewLine,
				actions.RestoreData
					.Select(a => a.Value)
					.OfType<SqlScriptAction>()
					.Select(a => a.Text));
	}
}
