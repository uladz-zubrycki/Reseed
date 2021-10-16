using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Reseed.Dsl;
using Reseed.Dsl.Cleanup;
using Reseed.Dsl.Simple;
using Reseed.Rendering;

namespace Reseed.Samples.NUnit
{
	// One of the possible ways to use Reseeder involves creation of base type for your test fixtures.
	// It will make sure that test database is initialized and cleaned up just once as well as
	// insert and delete test data before/after each test run. 
	[TestFixture]
	public class TestFixtureBase
	{
		private static readonly FixtureDatabase Database = new(
			GetRelativePath("Migrations"),
			GetRelativePath("Data"));
		
		protected static string ConnectionString => Database.ConnectionString;

		[OneTimeSetUp]
		public Task FixturesSetupAsync() => Database.OneTimeSetupAsync();

		[SetUp]
		public void Setup() => Database.TestSetupAsync();

		[TearDown]
		public void Teardown() => Database.TestTearDownAsync();

		[OneTimeTearDown]
		public Task FixturesTeardownAsync() => Database.OneTimeTeardownAsync();

		private static string GetRelativePath(string path) =>
			Path.Combine(TestContext.CurrentContext.TestDirectory, path);
	}

	// Encapsulates database initialization logic, which is sql docker container startup
	// and Reseeder scripts both creation and execution.
	// Might be not needed for your case, but felt natural here
	// as it allows us to have all the database related state and logic in one place.
	internal sealed class FixtureDatabase
	{
		private readonly string dbMigrationsPath;
		private readonly string reseederDataPath;
		private TestDatabase database;
		private Reseeder reseeder;
		private DbActions dbActions;

		public string ConnectionString => database?.ConnectionString;

		public FixtureDatabase(string dbMigrationsPath, string reseederDataPath)
		{
			this.dbMigrationsPath = dbMigrationsPath ?? throw new ArgumentNullException(nameof(dbMigrationsPath));
			this.reseederDataPath = reseederDataPath ?? throw new ArgumentNullException(nameof(reseederDataPath));
		}

		public async Task OneTimeSetupAsync()
		{
			database = new TestDatabase(dbMigrationsPath);
			await database.StartAsync();

			reseeder = new Reseeder(database.ConnectionString);
			dbActions = GenerateDbActions(reseeder, reseederDataPath);
			reseeder.Execute(dbActions.PrepareDatabase);
		}

		public Task TestSetupAsync()
		{
			reseeder.Execute(dbActions.InsertData);
			return Task.CompletedTask;
		}

		public Task TestTearDownAsync()
		{
			reseeder.Execute(dbActions.DeleteData);
			return Task.CompletedTask;
		}

		public async Task OneTimeTeardownAsync()
		{
			reseeder.Execute(dbActions.CleanupDatabase);
			if (database != null)
			{
				await database.DisposeAsync();
			}
		}

		// Configure Reseeder behavior. The simplest mode of behavior is used,
		// which leads to generation of plain sql scripts for Insert and Delete actions.
		private static DbActions GenerateDbActions(Reseeder seeder, string dataFolder) =>
			seeder.Generate(
				RenderMode.Simple(
					SimpleInsertDefinition.Script(),
					CleanupDefinition.Script(CleanupOptions.IncludeAll(CleanupKind.PreferTruncate()))),
				dataFolder);
	}
}
