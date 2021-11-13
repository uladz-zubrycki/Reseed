using System;
using System.Collections.Generic;
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
		private readonly string connectionString;

		public Reseeder([NotNull] string connectionString)
		{
			this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
		}

		public SeedActions Generate([NotNull] AnySeedMode mode)
		{
			if (mode == null) throw new ArgumentNullException(nameof(mode));
			
			var schemas = SchemaProvider.Load(connectionString);
			var orderedSchemas = NodeOrderer<TableSchema>.Order(schemas);

			return mode switch
			{
				CleanupOnlySeedMode cleanupOnly => SeedActionGenerator.Generate(cleanupOnly, orderedSchemas),
				SeedMode seedMode => Generate(seedMode, schemas, orderedSchemas),
				_ => throw new NotSupportedException($"Unknown seed mode '{mode.GetType().Name}'")
			};
		}

		public void Execute(
			[NotNull] IReadOnlyCollection<OrderedItem<ISeedAction>> actions,
			TimeSpan? actionTimeout = null)
		{
			if (actions == null) throw new ArgumentNullException(nameof(actions));
			SeedActionExecutor.Execute(this.connectionString, actions, actionTimeout);
		}

		private static SeedActions Generate(
			SeedMode mode, 
			IReadOnlyCollection<TableSchema> schemas, 
			OrderedGraph<TableSchema> orderedSchemas)
		{
			var entities = DataProvider.Load(mode.DataProvider);
			var tables = TableBuilder.Build(schemas, entities);
			var extendedTables = TableExtender.Extend(tables);
			DataValidator.Validate(extendedTables);

			var containers = TableOrderer.Order(extendedTables, orderedSchemas);
			return SeedActionGenerator.Generate(mode, orderedSchemas, containers);
		}
	}
}