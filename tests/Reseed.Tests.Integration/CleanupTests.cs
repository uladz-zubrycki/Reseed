using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using Reseed.Configuration;
using Reseed.Configuration.Cleanup;
using Reseed.Execution;
using Reseed.Generation;
using Reseed.Schema;
using Reseed.Tests.Integration.Core;

namespace Reseed.Tests.Integration
{
	[Parallelizable(ParallelScope.Fixtures)]
	public sealed class CleanupTests : TestFixtureBase
	{
		[Test]
		public async Task ShouldPreferDeleteForTableReferencedByIndexedView()
		{
			await using var database = await Conventional.CreateConventionalDatabase(this);
			var sql = new SqlEngine(database.ConnectionString);

			var reseeder = new Reseeder();
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

		[Test]
		public async Task ShouldRejectTruncateForTableReferencedByIndexedView()
		{
			await using var database = await Conventional.CreateConventionalDatabase(this);
			var reseeder = new Reseeder();

			var exception = Assert.Throws<InvalidOperationException>(() => reseeder.Generate(
				database.ConnectionString,
				new CleanupOnlySeedMode(CleanupDefinition.Script(
					CleanupMode.Truncate(),
					CleanupTarget.Excluding()))));

			Assert.That(exception.Message, Is.EqualTo(
				"Cleanup mode 'Truncate' can't truncate tables referenced by indexed views: dbo.User. " +
				"Add these tables to the useDeleteForTables argument or use CleanupMode.PreferTruncate()."));
		}

		[Test]
		public async Task ShouldUseDeleteIfExplicitlyRequested()
		{
			await using var database = await Conventional.CreateConventionalDatabase(this);
			var reseeder = new Reseeder();

			var actions = reseeder.Generate(
				database.ConnectionString,
				new CleanupOnlySeedMode(CleanupDefinition.Script(
					CleanupMode.Truncate(new[] { new ObjectName("User") }),
					CleanupTarget.Excluding())));

			Assert.That(
				GetCleanupScript(actions),
				Does.Contain("DELETE FROM [dbo].[User];"));
		}

		[Test]
		public async Task ShouldDropConstraintsToPreferTruncateForeignKeyTables()
		{
			await using var database = await Conventional.CreateConventionalDatabase(this);
			var sql = new SqlEngine(database.ConnectionString);
			var reseeder = new Reseeder();

			var actions = reseeder.Generate(
				database.ConnectionString,
				new CleanupOnlySeedMode(CleanupDefinition.Script(
					CleanupMode.PreferTruncate(
						constraintBehavior: ConstraintResolutionBehavior.DropConstraints),
					CleanupTarget.Excluding())));

			var cleanupScript = GetCleanupScript(actions);
			Assert.Multiple(() =>
			{
				Assert.That(cleanupScript, Does.Contain(
					"ALTER TABLE [dbo].[Child]"));
				Assert.That(cleanupScript, Does.Contain(
					"CONSTRAINT IF EXISTS [FK_Child_Parent]"));
				Assert.That(cleanupScript, Does.Contain(
					"CONSTRAINT [FK_Child_Parent]"));
				Assert.That(cleanupScript, Does.Contain(
					"TRUNCATE TABLE [dbo].[Parent];"));
				Assert.That(cleanupScript, Does.Contain(
					"TRUNCATE TABLE [dbo].[Child];"));
				Assert.That(cleanupScript, Does.Not.Contain("DELETE FROM"));
			});

			await using var connection = new SqlConnection(database.ConnectionString);
			await connection.OpenAsync();
			await using var command = connection.CreateCommand();

			command.CommandText = "SET XACT_ABORT OFF;";
			await command.ExecuteNonQueryAsync();
			reseeder.Execute(connection, actions.RestoreData);
			Assert.That(await GetXactAbortOption(command), Is.Zero);

			command.CommandText = "SET XACT_ABORT ON;";
			await command.ExecuteNonQueryAsync();
			reseeder.Execute(connection, actions.RestoreData);
			Assert.That(await GetXactAbortOption(command), Is.EqualTo(1));

			Assert.That(
				await CountForeignKey(sql),
				Is.EqualTo(1));
		}

		[Test]
		public async Task ShouldDropAndRecreateConstraintsForDelete()
		{
			await using var database = await Conventional.CreateConventionalDatabase(this);
			var sql = new SqlEngine(database.ConnectionString);
			var reseeder = new Reseeder();

			var actions = reseeder.Generate(
				database.ConnectionString,
				new CleanupOnlySeedMode(CleanupDefinition.Script(
					CleanupMode.Delete(ConstraintResolutionBehavior.DropConstraints),
					CleanupTarget.Excluding())));

			var cleanupScript = GetCleanupScript(actions);
			Assert.Multiple(() =>
			{
				Assert.That(cleanupScript, Does.Contain(
					"CONSTRAINT IF EXISTS [FK_Child_Parent]"));
				Assert.That(cleanupScript, Does.Contain(
					"CONSTRAINT [FK_Child_Parent]"));
				Assert.That(cleanupScript, Does.Not.Contain("NOCHECK"));
				Assert.That(cleanupScript, Does.Not.Contain("CHECK CONSTRAINT"));
			});

			reseeder.Execute(database.ConnectionString, actions.RestoreData);
			reseeder.Execute(database.ConnectionString, actions.RestoreData);

			Assert.That(
				await CountForeignKey(sql),
				Is.EqualTo(1));
		}

		[Test]
		public async Task ShouldPreferDeleteForIndexedViewWhenDroppingConstraints()
		{
			await using var database = await Conventional.CreateConventionalDatabase(this);
			var reseeder = new Reseeder();

			var actions = reseeder.Generate(
				database.ConnectionString,
				new CleanupOnlySeedMode(CleanupDefinition.Script(
					CleanupMode.PreferTruncate(
						constraintBehavior: ConstraintResolutionBehavior.DropConstraints),
					CleanupTarget.Excluding())));

			var cleanupScript = GetCleanupScript(actions);
			Assert.Multiple(() =>
			{
				Assert.That(cleanupScript, Does.Contain("DELETE FROM [dbo].[User];"));
				Assert.That(cleanupScript, Does.Not.Contain("TRUNCATE TABLE [dbo].[User];"));
			});
		}

		[Test]
		public async Task ShouldRecreateConstraintsAfterCustomCleanup()
		{
			await using var database = await Conventional.CreateConventionalDatabase(this);
			var sql = new SqlEngine(database.ConnectionString);
			var reseeder = new Reseeder();

			var actions = reseeder.Generate(
				database.ConnectionString,
				new CleanupOnlySeedMode(CleanupDefinition.Script(
					CleanupMode.Delete(ConstraintResolutionBehavior.DropConstraints),
					CleanupTarget.Excluding(
						customScripts: new[]
						{
							(
								table: new ObjectName("Child"),
								script: "DELETE FROM [dbo].[Child];")
						}))));

			reseeder.Execute(database.ConnectionString, actions.RestoreData);

			Assert.That(
				await CountForeignKey(sql),
				Is.EqualTo(1));
		}

		[Test]
		public async Task ShouldDropConstraintsForMutuallyReferencingTables()
		{
			await using var database = await Conventional.CreateConventionalDatabase(this);
			var sql = new SqlEngine(database.ConnectionString);
			var reseeder = new Reseeder();

			var actions = reseeder.Generate(
				database.ConnectionString,
				new CleanupOnlySeedMode(CleanupDefinition.Script(
					CleanupMode.Delete(ConstraintResolutionBehavior.DropConstraints),
					CleanupTarget.Excluding())));

			reseeder.Execute(database.ConnectionString, actions.RestoreData);
			reseeder.Execute(database.ConnectionString, actions.RestoreData);

			Assert.That(
				await sql.ExecuteScalarAsync<int>(
					"SELECT COUNT(1) FROM sys.foreign_keys " +
					"WHERE [name] IN ('FK_Left_Right', 'FK_Right_Left') " +
					"AND [is_disabled] = 0"),
				Is.EqualTo(2));
		}

		[Test]
		public async Task ShouldRollbackWhenConstraintCannotBeRecreated()
		{
			await using var database = await Conventional.CreateConventionalDatabase(this);
			var sql = new SqlEngine(database.ConnectionString);
			var reseeder = new Reseeder();

			var actions = reseeder.Generate(
				database.ConnectionString,
				new CleanupOnlySeedMode(CleanupDefinition.Script(
					CleanupMode.Delete(ConstraintResolutionBehavior.DropConstraints),
					CleanupTarget.Including(c =>
						c.IncludeTables(new ObjectName("Parent"))))));

			Assert.Throws<SeedActionExecutionException>(() =>
				reseeder.Execute(database.ConnectionString, actions.RestoreData));

			Assert.That(await CountForeignKey(sql), Is.EqualTo(1));
			Assert.That(
				await sql.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM [dbo].[Parent]"),
				Is.EqualTo(1));
		}

		private static Task<int> CountForeignKey(SqlEngine sql) =>
			sql.ExecuteScalarAsync<int>(
				"SELECT COUNT(1) FROM sys.foreign_keys " +
				"WHERE [name] = 'FK_Child_Parent' AND [is_disabled] = 0");

		private static async Task<int> GetXactAbortOption(SqlCommand command)
		{
			command.CommandText =
				"SELECT CASE WHEN (16384 & @@OPTIONS) = 16384 THEN 1 ELSE 0 END";
			return (int)await command.ExecuteScalarAsync();
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
