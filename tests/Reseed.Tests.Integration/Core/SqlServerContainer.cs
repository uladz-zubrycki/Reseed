using System;
using System.Threading.Tasks;
using DbUp;
using DbUp.Engine;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Configurations.Abstractions;
using DotNet.Testcontainers.Containers.Modules.Databases;
using DotNet.Testcontainers.Containers.WaitStrategies;

namespace Reseed.Tests.Integration.Core
{
	public sealed class SqlServerContainer: IAsyncDisposable
	{
		private readonly string scriptsFolder;
		private readonly Func<string, bool> scriptFilter;
		private readonly MsSqlTestcontainer server;

		public string ConnectionString => server.ConnectionString;

		public SqlServerContainer(string scriptsFolder)
			: this(scriptsFolder, _ => true)
		{
		}

		public SqlServerContainer(string scriptsFolder, Func<string, bool> scriptFilter)
		{
			this.scriptsFolder = scriptsFolder ?? throw new ArgumentNullException(nameof(scriptsFolder));
			this.scriptFilter = scriptFilter ?? throw new ArgumentNullException(nameof(scriptFilter));
			this.server = CreateDatabaseContainer();
		}

		public async Task StartAsync()
		{
			await StartServerAsync();
			EnsureDatabase.For.SqlDatabase(server.ConnectionString);

			var migrationResult = MigrateDatabase(
				server.ConnectionString,
				scriptsFolder,
				scriptFilter);

			if (!migrationResult.Successful)
			{
				throw new InvalidOperationException("Can't apply database migrations", migrationResult.Error);
			}
		}

		private async Task StartServerAsync()
		{
			try
			{
				await server.StartAsync();
			}
			catch (TimeoutException ex)
			{
				throw new InvalidOperationException(
					"Can't start sql server container, make sure docker is running",
					ex);
			}
		}

		public ValueTask DisposeAsync() => 
			this.server.DisposeAsync();

		private static MsSqlTestcontainer CreateDatabaseContainer() =>
			new TestcontainersBuilder<MsSqlTestcontainer>()
				.WithDatabase(new DbContainerConfiguration("MsSqlContainerDb", "!A1B2c3d4_"))
				.WithName($"sql-db_{Guid.NewGuid()}")
				.Build();

		private static DatabaseUpgradeResult MigrateDatabase(
			string connectionString,
			string scriptsPath,
			Func<string, bool> scriptFilter)
		{
			var upgrade = DeployChanges.To
				.SqlDatabase(connectionString, "dbo")
				.WithScriptsFromFileSystem(scriptsPath, scriptFilter)
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
