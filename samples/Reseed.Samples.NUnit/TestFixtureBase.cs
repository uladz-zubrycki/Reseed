using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Reseed.Configuration;
using Reseed.Configuration.Cleanup;
using Reseed.Configuration.Simple;
using Reseed.Data;
using Reseed.Generation;

namespace Reseed.Samples.NUnit
{
	// One of the possible ways to use Reseeder involves creation of base type for your test fixtures.
	// It will make sure that test database is initialized and cleaned up just once as well as
	// insert and delete test data before/after each test run. 
	[TestFixture]
	public class TestFixtureBase
	{
		private static readonly TestDatabase Database = new(
			GetRelativePath("Migrations"),
			GetRelativePath("Data"));
		
		protected static string ConnectionString => Database.ConnectionString;

		[OneTimeSetUp]
		public Task FixturesSetupAsync() => Database.InitializeAsync();

		[SetUp]
		public void Setup() => Database.InsertDataAsync();

		[TearDown]
		public void Teardown() => Database.DeleteDataAsync();

		[OneTimeTearDown]
		public Task FixturesTeardownAsync() => Database.CleanupAsync();

		private static string GetRelativePath(string path) =>
			Path.Combine(TestContext.CurrentContext.TestDirectory, path);
	}

	// Encapsulates database initialization logic, which is sql docker container startup
	// and Reseeder scripts both creation and execution.
	// Might be not needed for your case, but felt natural here
	// as it allows us to have all the database related state and logic in one place.
	internal sealed class TestDatabase
	{
		private readonly string dbMigrationsPath;
		private readonly string reseederDataPath;
		private SqlServerContainer server;
		private Reseeder reseeder;
		private SeedActions seedActions;

		public string ConnectionString => server?.ConnectionString;

		public TestDatabase(string dbMigrationsPath, string reseederDataPath)
		{
			this.dbMigrationsPath = dbMigrationsPath ?? throw new ArgumentNullException(nameof(dbMigrationsPath));
			this.reseederDataPath = reseederDataPath ?? throw new ArgumentNullException(nameof(reseederDataPath));
		}

		public async Task InitializeAsync()
		{
			server = new SqlServerContainer(dbMigrationsPath);
			await server.StartAsync();

			reseeder = new Reseeder(server.ConnectionString);
			seedActions = GenerateSeedActions(reseeder, reseederDataPath);
			reseeder.Execute(seedActions.PrepareDatabase);
		}

		public Task InsertDataAsync()
		{
			reseeder.Execute(seedActions.InsertData);
			return Task.CompletedTask;
		}

		public Task DeleteDataAsync()
		{
			reseeder.Execute(seedActions.DeleteData);
			return Task.CompletedTask;
		}

		public async Task CleanupAsync()
		{
			reseeder.Execute(seedActions.CleanupDatabase);
			if (server != null)
			{
				await server.DisposeAsync();
			}
		}

		// Configure Reseeder behavior. The simplest mode of behavior is used,
		// which leads to generation of plain sql scripts for Insert and Delete actions.
		private static SeedActions GenerateSeedActions(Reseeder seeder, string dataFolder) =>
			seeder.Generate(
				SeedMode.Simple(
					SimpleInsertDefinition.Script(),
					CleanupDefinition.Script(CleanupOptions.IncludeAll(CleanupKind.PreferTruncate()))),
				DataProvider.Xml(dataFolder));
	}
}
