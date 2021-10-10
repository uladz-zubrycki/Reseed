using System.IO;
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
		protected static readonly string ConnectionString = GetConnectionString();
		private static readonly string DataFolder = GetDataFolder();
		private static readonly Reseeder Reseeder = new(ConnectionString);
		private readonly DbActions actions;

		public TestFixtureBase()
		{
			actions = Reseeder.Generate(
				RenderMode.Simple(
					SimpleInsertDefinition.Script(),
					CleanupDefinition.Script(CleanupOptions.IncludeAll(CleanupKind.PreferTruncate()))),
				DataFolder);
		}

		[OneTimeSetUp]
		public void FixturesSetup()
		{
			Reseeder.Execute(actions.PrepareDatabase);
		}

		[SetUp]
		public void Setup()
		{
			Reseeder.Execute(actions.InsertData);
		}

		[TearDown]
		public void Teardown()
		{
			Reseeder.Execute(actions.DeleteData);
		}

		[OneTimeTearDown]
		public void FixturesTeardown()
		{
			Reseeder.Execute(actions.CleanupDatabase);
		}

		// Normally you would get it from configuration file
		private static string GetConnectionString() => 
			"Data Source=.; Initial Catalog=Reseed.Samples.NUnit; Integrated Security=true";

		private static string GetDataFolder() =>
			Path.Combine(
				TestContext.CurrentContext.TestDirectory,
				"Data");
	}
}
