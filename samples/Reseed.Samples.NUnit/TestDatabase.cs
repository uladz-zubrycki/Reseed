using System;
using System.Threading.Tasks;
using DbUp;
using DbUp.Engine;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Configurations.Abstractions;
using DotNet.Testcontainers.Containers.Modules.Databases;
using DotNet.Testcontainers.Containers.WaitStrategies;

namespace Reseed.Samples.NUnit
{
	public class TestDatabase: IAsyncDisposable
	{
		private readonly string scriptsFolder;
		private readonly MsSqlTestcontainer server;

		public string ConnectionString => server.ConnectionString;

		public TestDatabase(string scriptsFolder)
		{
			this.scriptsFolder = scriptsFolder ?? throw new ArgumentNullException(nameof(scriptsFolder));
			this.server = CreateDatabaseContainer();
		}

		public async Task StartAsync()
		{
			await server.StartAsync();
			EnsureDatabase.For.SqlDatabase(server.ConnectionString);

			var migrationResult = MigrateDatabase(server.ConnectionString, scriptsFolder);
			if (!migrationResult.Successful)
			{
				throw new InvalidOperationException("Can't apply database migrations", migrationResult.Error);
			}
		}

		public ValueTask DisposeAsync() => 
			this.server.DisposeAsync();

		private static MsSqlTestcontainer CreateDatabaseContainer() =>
			new TestcontainersBuilder<MsSqlTestcontainer>()
				.WithDatabase(new DbContainerConfiguration("MsSqlContainerDb", "!A1B2c3d4_"))
				.WithName("sql-db")
				.Build();

		private static DatabaseUpgradeResult MigrateDatabase(string connectionString, string scriptsPath)
		{
			var upgrade = DeployChanges.To
				.SqlDatabase(connectionString, "dbo")
				.WithScriptsFromFileSystem(scriptsPath)
				.LogToConsole()
				.WithTransactionPerScript()
				.Build();
			
			return upgrade.PerformUpgrade();
		}
	}

	public sealed class DbContainerConfiguration : TestcontainerDatabaseConfiguration
	{
		private const string ServerImage = "mcr.microsoft.com/mssql/server:latest";
		private const string PasswordKey = "SA_PASSWORD";
		private const string EulaKey = "ACCEPT_EULA";
		private const int ServerPort = 1433;

		public DbContainerConfiguration(string database, string password) : base(ServerImage, ServerPort)
		{
			Environments[EulaKey] = "Y";
			Password = password;
			Database = database;
		}

		public override string Username => "sa";

		public override string Password
		{
			get => Environments[PasswordKey];
			set => Environments[PasswordKey] = value;
		}

		public override IWaitForContainerOS WaitStrategy => Wait
			.ForUnixContainer()
			.UntilPortIsAvailable(ServerPort);
	}
}
