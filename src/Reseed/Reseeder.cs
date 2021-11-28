using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Configuration;
using Reseed.Data;
using Reseed.Execution;
using Reseed.Extension;
using Reseed.Generation;
using Reseed.Generation.Schema;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Schema;
using Reseed.Validation;

namespace Reseed
{
	[PublicAPI]
	public sealed class Reseeder
	{
		private readonly ReseederOptions options;

		public Reseeder() : this(ReseederOptions.Default)
		{
		}

		public Reseeder([NotNull] ReseederOptions options)
		{
			this.options = options ?? throw new ArgumentNullException(nameof(options));
		}

		public SeedActions Generate([NotNull] SqlConnection connection, [NotNull] AnySeedMode mode)
		{
			AssertConnection(connection);
			if (mode == null) throw new ArgumentNullException(nameof(mode));

			var schemas = SchemaProvider.Load(connection);
			var orderedSchemas = NodeOrderer<TableSchema>.Order(schemas);

			return mode switch
			{
				CleanupOnlySeedMode cleanupOnly => SeedActionGenerator.Generate(cleanupOnly, orderedSchemas),
				SeedMode seedMode => Generate(seedMode, schemas, orderedSchemas, this.options),
				_ => throw new NotSupportedException($"Unknown seed mode '{mode.GetType().Name}'")
			};
		}

		public void Execute(
			[NotNull] SqlConnection connection,
			[NotNull] IReadOnlyCollection<OrderedItem<ISeedAction>> actions,
			TimeSpan? actionTimeout = null)
		{
			AssertConnection(connection);
			if (actions == null) throw new ArgumentNullException(nameof(actions));
			SeedActionExecutor.Execute(connection, actions, actionTimeout);
		}

		private static SeedActions Generate(
			SeedMode mode,
			IReadOnlyCollection<TableSchema> schemas,
			OrderedGraph<TableSchema> orderedSchemas,
			ReseederOptions options)
		{
			var entities = DataProvider.Load(mode.DataProviders);
			var tables = TableBuilder.Build(schemas, entities);
			var extendedTables = TableExtender.Extend(tables, options.ExtensionOptions);

			if (options.ValidateData)
			{
				DataValidator.Validate(extendedTables);
			}

			var containers = TableOrderer.Order(extendedTables, orderedSchemas);
			return SeedActionGenerator.Generate(mode, orderedSchemas, containers);
		}

		private static void AssertConnection(SqlConnection connection)
		{
			if (connection == null) throw new ArgumentNullException(nameof(connection));
			if (connection.State != ConnectionState.Open)
			{
				throw new ArgumentException(
					$"The provided connection must be already opened, while received connection in a '{connection.State}' state");
			}
		}
	}
}