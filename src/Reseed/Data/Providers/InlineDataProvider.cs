using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Schema;

namespace Reseed.Data.Providers
{
	internal sealed class InlineDataProvider : IVerboseDataProvider
	{
		private readonly IReadOnlyCollection<Entity> entities;

		public InlineDataProvider([NotNull] IReadOnlyCollection<Entity> entities)
		{
			if (entities == null) throw new ArgumentNullException(nameof(entities));
			if (entities.Count == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(entities));
			
			this.entities = entities;
		}

		public IReadOnlyCollection<Entity> GetEntities() =>
			entities.ToArray();

		public VerboseDataProviderResult GetEntitiesDetailed() =>
			new(entities,
				entities.Select(e => e.Origin).Distinct().ToArray(),
				Array.Empty<EntityOrigin>());
	}

	public sealed class InlineDataProviderBuilder 
	{
		private readonly List<Entity> entities = new();

		public InlineDataProviderBuilder AddEntities([NotNull] params Entity[] rows)
		{
			if (rows == null) throw new ArgumentNullException(nameof(rows));

			entities.AddRange(rows.Select(e => new Entity(
				new InlineEntityOrigin(e.Name),
				e.Name,
				e.Properties)));

			return this;
		}

		public InlineDataProviderBuilder AddEntities(
			[NotNull] ObjectName name,
			[NotNull] params Property[][] items)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (items == null) throw new ArgumentNullException(nameof(items));

			var entityName = name.GetSqlName(false);
			entities.AddRange(
				items.Select(r => new Entity(
					new InlineEntityOrigin(entityName),
					entityName,
					r)));

			return this;
		}

		public InlineDataProviderBuilder AddEntities(
			[NotNull] ObjectName name,
			[NotNull] IEnumerable<IDictionary<string, string>> items)
		{
			if (items == null) throw new ArgumentNullException(nameof(items));
			return AddEntities(name,
				items.Select(r => r
						.Select(kv => new Property(kv.Key, kv.Value))
						.ToArray())
					.ToArray());
		}

		public IDataProvider Build()
		{
			if (entities.Count == 0)
			{
				throw new InvalidOperationException(
					$"Invalid {nameof(InlineDataProviderBuilder)} configuration," +
					"at least one entity should be added, but found none");
			}

			return new InlineDataProvider(this.entities);
		}
	}

	internal sealed class InlineEntityOrigin : EntityOrigin
	{
		private readonly string tableName;

		public InlineEntityOrigin([NotNull] string tableName)
		{
			this.tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
		}

		public override string OriginName => $"Inline '{tableName}'";
	}
}
