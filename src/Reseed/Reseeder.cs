using System;
using System.Collections.Generic;
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
		private readonly string connectionString;

		public Reseeder([NotNull] string connectionString)
		{
			this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
		}

		public SeedActions Generate(
			[NotNull] SeedMode mode,
			[NotNull] IDataProvider dataProvider)
		{
			if (mode == null) throw new ArgumentNullException(nameof(mode));
			if (dataProvider == null) throw new ArgumentNullException(nameof(dataProvider));

			var entities = DataProvider.Load(dataProvider);
			var schemas = SchemaProvider.Load(connectionString);
			var tables = TableBuilder.Build(schemas, entities);
			var extendedTables = TableExtender.Extend(tables);
			DataValidator.Validate(extendedTables);

			var orderedSchemas = NodeOrderer<TableSchema>.Order(schemas);
			var containers = TableOrderer.Order(extendedTables, orderedSchemas);
			return SeedActionGenerator.Generate(orderedSchemas, containers, mode);
		}

		public void Execute(
			[NotNull] IReadOnlyCollection<OrderedItem<ISeedAction>> actions,
			TimeSpan? actionTimeout = null)
		{
			if (actions == null) throw new ArgumentNullException(nameof(actions));
			SeedActionExecutor.Execute(this.connectionString, actions, actionTimeout);
		}
	}
}