using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using Reseed.Configuration;
using Reseed.Configuration.Basic;
using Reseed.Configuration.Cleanup;
using Reseed.Data;
using Reseed.Generation;
using Reseed.Generation.Insertion;
using Reseed.Generation.Schema;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Schema;
using Reseed.Schema.Providers;
using Reseed.Tests.Integration.Core;
using Testing.Common.Api.Schema;

namespace Reseed.Tests.Integration
{
	[Parallelizable(ParallelScope.Fixtures)]
	public sealed class InvitationForeignKeyOrderTests
	{
		private const string DataFolder = "Data/InvitationForeignKeyOrderTests";

		[Test]
		public async Task ShouldInsertWithForeignKeysEnabledUsingPublicGenerate()
		{
			await using var database = await CreateDatabase();
			using var connection = new SqlConnection(database.ConnectionString);
			await connection.OpenAsync();

			var actions = new Reseeder().Generate(connection, SeedMode.Basic(
				BasicInsertDefinition.Script(),
				CleanupDefinition.NoCleanup(),
				SeedData("Seed.xml")));
			var script = GetInsertScript(actions);

			Assert.That(script, Does.Not.Contain("NOCHECK"));
			AssertUserBeforeInvitation(script);
			new Reseeder().Execute(connection, actions.RestoreData);
			await AssertRowsInserted(database);
		}

		[TestCase("Invitation", true)]
		[TestCase("Invitation", false)]
		[TestCase("User", true)]
		[TestCase("User", false)]
		public async Task ShouldOrderEveryInvitationForeignKeyRegardlessOfTraversal(
			string firstTable,
			bool optionalFirst)
		{
			await using var database = await CreateDatabase();
			using var connection = new SqlConnection(database.ConnectionString);
			await connection.OpenAsync();

			var script = GenerateInsertScript(connection, firstTable, optionalFirst, "Seed.xml");
			Assert.That(script, Does.Not.Contain("NOCHECK"));
			AssertUserBeforeInvitation(script);

			await new SqlEngine(database.ConnectionString).ExecuteNonQueryAsync(script);
			await AssertRowsInserted(database);
		}

		[TestCase(true)]
		[TestCase(false)]
		public async Task ShouldIncludeEveryForeignKeyInARealRowCycle(bool optionalFirst)
		{
			await using var database = await CreateDatabase();
			using var connection = new SqlConnection(database.ConnectionString);
			await connection.OpenAsync();

			var script = GenerateInsertScript(connection, "Invitation", optionalFirst, "SeedCycle.xml");
			Assert.Multiple(() =>
			{
				Assert.That(script, Does.Contain("NOCHECK CONSTRAINT [FK_Invitation_User]"));
				Assert.That(script, Does.Contain("NOCHECK CONSTRAINT [FK_Invitation_Inviter]"));
				Assert.That(script, Does.Contain("NOCHECK CONSTRAINT [FK_User_Invitation]"));
			});

			await new SqlEngine(database.ConnectionString).ExecuteNonQueryAsync(script);
			var sql = new SqlEngine(database.ConnectionString);
			Assert.That(await sql.ExecuteScalarAsync<int>(
				"SELECT COUNT(*) FROM [dbo].[Invitation] WHERE [Id] = 100000 AND [CreatorId] = 118 AND [UserId] = 118"),
				Is.EqualTo(1));
			Assert.That(await sql.ExecuteScalarAsync<int>(
				"SELECT COUNT(*) FROM [dbo].[User] WHERE [Id] = 118 AND [InvitationId] = 100000"),
				Is.EqualTo(1));
			await AssertForeignKeysEnabled(database);
		}

		private static string GenerateInsertScript(
			SqlConnection connection,
			string firstTable,
			bool optionalFirst,
			string xmlFile)
		{
			var tables = MsSqlSchemaReader.LoadTables(connection)
				.OrderBy(t => t.Name.Name == firstTable ? 0 : 1)
				.ToArray();
			var foreignKeys = MsSqlSchemaReader.LoadForeignKeys(connection, tables)
				.OrderBy(r => r.Association.Name ==
					(optionalFirst ? "FK_Invitation_User" : "FK_Invitation_Inviter") ? 0 : 1)
				.ToArray();
			var schemas = NodeBuilder<TableSchema>.CollectNodes(
				tables,
				foreignKeys,
				(reference, target) => reference.Map(_ => target),
				(table, references) => new TableSchema(
					table.Name,
					table.Columns,
					table.PrimaryKey,
					references,
					table.IsReferencedByIndexedView));
			var invitation = schemas.Single(s => s.Name.Name == "Invitation");
			var invitationReferences = invitation.References
				.OrderBy(r => r.Association.Name ==
					(optionalFirst ? "FK_Invitation_User" : "FK_Invitation_Inviter") ? 0 : 1)
				.ToArray();
			((ICollection<Reference<TableSchema>>)invitation.References).Clear();
			invitation.AddReferences(invitationReferences);
			Assert.That(schemas.First().Name.Name, Is.EqualTo(firstTable));
			Assert.That(invitation.References.First().Association.Name,
				Is.EqualTo(optionalFirst ? "FK_Invitation_User" : "FK_Invitation_Inviter"));
			var orderedSchemas = NodeOrderer<TableSchema>.Order(schemas);
			var entities = DataProvider.Load(new[] { SeedData(xmlFile) });
			var seedTables = TableBuilder.Build(schemas, entities);
			var containers = TableOrderer.Order(seedTables, orderedSchemas);
			return InsertScriptRenderer.Render(containers).Text;
		}

		private static IDataProvider SeedData(string fileName) =>
			DataProviders.Xml(DataFolder, path =>
				string.Equals(System.IO.Path.GetFileName(path), fileName, StringComparison.Ordinal));

		private static string GetInsertScript(SeedActions actions) =>
			string.Join(Environment.NewLine, actions.RestoreData
				.OrderBy(action => action.Order)
				.Select(action => action.Value)
				.OfType<SqlScriptAction>()
				.Select(action => action.Text));

		private static void AssertUserBeforeInvitation(string script)
		{
			var userIndex = script.IndexOf("INSERT INTO [dbo].[User]", StringComparison.Ordinal);
			var invitationIndex = script.IndexOf("INSERT INTO [dbo].[Invitation]", StringComparison.Ordinal);
			Assert.That(userIndex, Is.GreaterThanOrEqualTo(0));
			Assert.That(invitationIndex, Is.GreaterThan(userIndex));
		}

		private static async Task AssertRowsInserted(SqlServerContainer database)
		{
			var sql = new SqlEngine(database.ConnectionString);
			Assert.That(await sql.ExecuteScalarAsync<int>(
				"SELECT COUNT(*) FROM [dbo].[User] WHERE [Id] = 118 AND [InvitationId] IS NULL"),
				Is.EqualTo(1));
			Assert.That(await sql.ExecuteScalarAsync<int>(
				"SELECT COUNT(*) FROM [dbo].[Invitation] WHERE [Id] = 100000 AND [CreatorId] = 118 AND [UserId] IS NULL"),
				Is.EqualTo(1));
			await AssertForeignKeysEnabled(database);
			Assert.That(await sql.ExecuteScalarAsync<int>(
				"SELECT COUNT(*) FROM [sys].[foreign_keys] WHERE [is_not_trusted] = 0"),
				Is.EqualTo(3));
		}

		private static async Task AssertForeignKeysEnabled(SqlServerContainer database)
		{
			var sql = new SqlEngine(database.ConnectionString);
			Assert.That(await sql.ExecuteScalarAsync<int>(
				"SELECT COUNT(*) FROM [sys].[foreign_keys] WHERE [is_disabled] = 0"),
				Is.EqualTo(3));
		}

		private static async Task<SqlServerContainer> CreateDatabase()
		{
			var database = new SqlServerContainer(DataFolder, path =>
				string.Equals(System.IO.Path.GetFileName(path), "Schema.sql", StringComparison.Ordinal));
			await database.StartAsync();
			return database;
		}
	}
}
