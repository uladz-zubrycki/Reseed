﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Reseed.Configuration;
using Reseed.Configuration.Basic;
using Reseed.Configuration.Cleanup;
using Reseed.Data;
using Reseed.Generation;
using Reseed.Schema;

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
		public void Setup() => Database.RestoreDataAsync();

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

			reseeder = new Reseeder();
			seedActions = GenerateSeedActions(reseederDataPath);
			reseeder.Execute(server.ConnectionString, seedActions.PrepareDatabase);
		}

		public Task RestoreDataAsync()
		{
			if (seedActions != null)
			{
				reseeder.Execute(server.ConnectionString, seedActions.RestoreData);
			}

			return Task.CompletedTask;
		}

		public async Task CleanupAsync()
		{
			if (seedActions != null)
			{
				reseeder.Execute(server.ConnectionString, seedActions.CleanupDatabase);
			}

			if (server != null)
			{
				await server.DisposeAsync();
			}
		}

		// Configure Reseeder behavior. The simplest mode of behavior is used,
		// which leads to generation of plain sql scripts for Insert and Delete actions.
		private SeedActions GenerateSeedActions(string dataFolder) =>
			reseeder.Generate(
				server.ConnectionString,
				SeedMode.Basic(
					BasicInsertDefinition.Script(),
					CleanupDefinition.Script(
						CleanupMode.PreferTruncate(),
						CleanupTarget.Excluding()),
					DataProviders.Xml(dataFolder),
					DataProviders.Inline(builder =>
						builder
							.AddEntities(
								new Entity("User", new[]
								{
									new Property("Id", "2"),
									new Property("FirstName", "Alice"),
									new Property("LastName", "Freeman"),
								}))
							.AddEntities(
								new ObjectName("User", "dbo"),
								new[]
								{
									new Property("FirstName", "Bob"),
									new Property("LastName", "Spencer"),
								})
							.AddEntities(
								new ObjectName("User"),
								new[]
								{
									new Dictionary<string, string>
									{
										["FirstName"] = "Jenny",
										["LastName"] = "Lee"
									}
								})
							.Build())));
	}
}