using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Ordering;

namespace Reseed.Rendering
{
	[PublicAPI]
	public sealed class DbActions
	{
		public readonly IReadOnlyCollection<OrderedItem<IDbAction>> PrepareDatabase;
		public readonly IReadOnlyCollection<OrderedItem<IDbAction>> InsertData;
		public readonly IReadOnlyCollection<OrderedItem<IDbAction>> DeleteData;
		public readonly IReadOnlyCollection<OrderedItem<IDbAction>> CleanupDatabase;

		internal DbActions(
			[NotNull] IReadOnlyCollection<OrderedItem<IDbAction>> prepareDatabase,
			[NotNull] IReadOnlyCollection<OrderedItem<IDbAction>> insertData,
			[NotNull] IReadOnlyCollection<OrderedItem<IDbAction>> deleteData,
			[NotNull] IReadOnlyCollection<OrderedItem<IDbAction>> cleanupDatabase)
		{
			this.PrepareDatabase = prepareDatabase ?? throw new ArgumentNullException(nameof(prepareDatabase));
			this.InsertData = insertData ?? throw new ArgumentNullException(nameof(insertData));
			this.DeleteData = deleteData ?? throw new ArgumentNullException(nameof(deleteData));
			this.CleanupDatabase = cleanupDatabase ?? throw new ArgumentNullException(nameof(cleanupDatabase));
		}
	}

	internal enum DbActionStage
	{
		PrepareDb,
		Insert,
		Delete,
		CleanupDb
	}

	internal sealed class DbActionsBuilder
	{
		private readonly List<(DbActionStage stage, IDbAction action)> items =
			new();

		public DbActionsBuilder Add<T>(DbActionStage stage, [NotNull] params T[] actions)
			where T : IDbAction => Add(stage, actions.AsEnumerable());

		public DbActionsBuilder Add<T>(DbActionStage stage, [NotNull] IEnumerable<OrderedItem<T>> actions)
			where T : IDbAction => Add(stage, actions.Order());

		public DbActionsBuilder Add<T>(DbActionStage stage, [NotNull] IEnumerable<T> actions)
			where T : IDbAction
		{
			if (actions == null) throw new ArgumentNullException(nameof(actions));

			foreach (var action in actions)
			{
				this.items.Add((stage, action));
			}

			return this;
		}

		public DbActions Build()
		{
			var stages = this.items
				.WithNaturalOrder()
				.GroupBy(x => x.Value.stage)
				.ToDictionary(gr => gr.Key,
					gr => gr
						.Select(ox => ox.Map(x => x.action))
						.Order()
						.WithNaturalOrder()
						.ToArray());

			return new DbActions(
				Get(DbActionStage.PrepareDb),
				Get(DbActionStage.Insert),
				Get(DbActionStage.Delete),
				Get(DbActionStage.CleanupDb));

			OrderedItem<IDbAction>[] Get(DbActionStage stage) =>
				stages.TryGetValue(stage, out var acts)
					? acts
					: Array.Empty<OrderedItem<IDbAction>>();
		}
	}
}