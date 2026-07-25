using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Reseed.Extension;
using Reseed.Generation.Schema;
using Reseed.Tests.Integration.Core;

namespace Reseed.Tests.Integration
{
	[Parallelizable(ParallelScope.Fixtures)]
	public sealed class CustomTableExtensionTests : TestFixtureBase
	{
		[Test]
		public async Task ShouldApplyCustomTableExtension()
		{
			await using var database = await Conventional.CreateConventionalDatabase(this);
			var reseeder = new Reseeder(new ReseederOptions(
				true,
				new DataExtensionOptions(
					true,
					new[] { new UserNameTableExtension() })));
			var actions = reseeder.Generate(
				database.ConnectionString,
				SeedModes.BasicScriptPreferTruncate(
					Conventional.CreateConventionalDataProvider(this)));

			reseeder.Execute(database.ConnectionString, actions.RestoreData);

			var sqlEngine = new SqlEngine(database.ConnectionString);
			var customNames = await sqlEngine.ExecuteScalarAsync<int>(
				"SELECT COUNT(1) FROM [dbo].[User] WHERE FirstName = 'Custom'");
			Assert.That(customNames, Is.EqualTo(1));
		}

		private sealed class UserNameTableExtension : ITableExtension
		{
			public Table Extend(Table table) =>
				table.MapRows(row =>
				{
					if (row.GetValue("Id") == null)
						throw new InvalidOperationException(
							"Built-in extensions should run before custom extensions.");

					return row.WithValue("FirstName", "Custom");
				});
		}
	}
}
