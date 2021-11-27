using System;
using System.IO;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using Reseed.Configuration;
using Reseed.Data;

namespace Reseed.Tests.Integration.Core
{
	public static class Conventional
	{
		public static async Task<SqlServerContainer> CreateConventionalDatabase(TestFixtureBase fixture)
		{
			var scriptsFolder = GetDataFolder(fixture);
			var database = new SqlServerContainer(
				scriptsFolder,
				GetTestDataFileFilter("sql"));
			await database.StartAsync();
			
			return database;
		}

		public static IDataProvider CreateConventionalDataProvider(TestFixtureBase fixture) =>
			DataProviders.Xml(GetDataFolder(fixture), GetTestDataFileFilter("xml"));

		public static async Task AssertSeedSucceeds(
			TestFixtureBase fixture,
			Func<IDataProvider, SeedMode> createSeedMode,
			Func<SqlEngine, Task> modifyData,
			Func<SqlEngine, Task> assertDataRestored)
		{
			await using var database = await CreateConventionalDatabase(fixture);
			var reseeder = new Reseeder();
			var sqlEngine = new SqlEngine(database.ConnectionString);
			var actions = reseeder.Generate(
				database.ConnectionString, 
				createSeedMode(CreateConventionalDataProvider(fixture)));

			reseeder.Execute(database.ConnectionString, actions.PrepareDatabase);
			
			reseeder.Execute(database.ConnectionString, actions.RestoreData);
			await assertDataRestored(sqlEngine);

			await modifyData(sqlEngine);
			reseeder.Execute(database.ConnectionString, actions.RestoreData);
			await assertDataRestored(sqlEngine);

			reseeder.Execute(database.ConnectionString, actions.CleanupDatabase);
		}

		public static async Task AssertGenerationFails(
			TestFixtureBase fixture,
			Func<IDataProvider, SeedMode> createSeedMode,
			Expression<Func<Exception, bool>> assertError)
		{
			await using var database = await CreateConventionalDatabase(fixture);
			var reseeder = new Reseeder();
			var assertErrorFun = assertError.Compile();

			try
			{
				_ = reseeder.Generate(
					database.ConnectionString,
					createSeedMode(CreateConventionalDataProvider(fixture)));
			}
			catch (Exception ex) when (assertErrorFun(ex))
			{
				Assert.Pass();
			}
			catch (Exception ex)
			{
				Assert.Fail(
					"Generation failed with unexpected exception." +
					$"Expected exception matching filter '{assertError}', " +
					$"but got '{ex}'");
			}
		}

		private static Func<string, bool> GetTestDataFileFilter(
			string fileExtension)
		{
			var testName = TestContext.CurrentContext.Test.MethodName;
			return s => Regex.IsMatch(s, $"(\\d+_)?{testName}.{fileExtension}");
		}

		private static string GetDataFolder(TestFixtureBase fixture) => Path.Combine(
			"Data",
			fixture.GetType().Name);
	}
}
