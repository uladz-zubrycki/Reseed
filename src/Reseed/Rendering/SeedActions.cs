using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Ordering;

namespace Reseed.Rendering
{
	[PublicAPI]
	public sealed class SeedActions
	{
		public readonly IReadOnlyCollection<OrderedItem<ISeedAction>> PrepareDatabase;
		public readonly IReadOnlyCollection<OrderedItem<ISeedAction>> InsertData;
		public readonly IReadOnlyCollection<OrderedItem<ISeedAction>> DeleteData;
		public readonly IReadOnlyCollection<OrderedItem<ISeedAction>> CleanupDatabase;

		internal SeedActions(
			[NotNull] IReadOnlyCollection<OrderedItem<ISeedAction>> prepareDatabase,
			[NotNull] IReadOnlyCollection<OrderedItem<ISeedAction>> insertData,
			[NotNull] IReadOnlyCollection<OrderedItem<ISeedAction>> deleteData,
			[NotNull] IReadOnlyCollection<OrderedItem<ISeedAction>> cleanupDatabase)
		{
			this.PrepareDatabase = prepareDatabase ?? throw new ArgumentNullException(nameof(prepareDatabase));
			this.InsertData = insertData ?? throw new ArgumentNullException(nameof(insertData));
			this.DeleteData = deleteData ?? throw new ArgumentNullException(nameof(deleteData));
			this.CleanupDatabase = cleanupDatabase ?? throw new ArgumentNullException(nameof(cleanupDatabase));
		}
	}

	internal enum SeedStage
	{
		PrepareDb,
		Insert,
		Delete,
		CleanupDb
	}

	internal sealed class SeedActionsBuilder
	{
		private readonly List<(SeedStage stage, ISeedAction action)> items =
			new();

		public SeedActionsBuilder Add<T>(SeedStage stage, [NotNull] params T[] actions)
			where T : ISeedAction => Add(stage, actions.AsEnumerable());

		public SeedActionsBuilder Add<T>(SeedStage stage, [NotNull] IEnumerable<OrderedItem<T>> actions)
			where T : ISeedAction => Add(stage, actions.Order());

		public SeedActionsBuilder Add<T>(SeedStage stage, [NotNull] IEnumerable<T> actions)
			where T : ISeedAction
		{
			if (actions == null) throw new ArgumentNullException(nameof(actions));

			foreach (var action in actions)
			{
				this.items.Add((stage, action));
			}

			return this;
		}

		public SeedActions Build()
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

			return new SeedActions(
				Get(SeedStage.PrepareDb),
				Get(SeedStage.Insert),
				Get(SeedStage.Delete),
				Get(SeedStage.CleanupDb));

			OrderedItem<ISeedAction>[] Get(SeedStage stage) =>
				stages.TryGetValue(stage, out var acts)
					? acts
					: Array.Empty<OrderedItem<ISeedAction>>();
		}
	}
}