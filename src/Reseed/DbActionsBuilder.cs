﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Ordering;

namespace Reseed
{
	internal sealed class DbActionsBuilder
	{
		private readonly List<(DbActionStage stage, IDbAction action)> items =
			new List<(DbActionStage, IDbAction)>();

		public DbActionsBuilder Append(DbActionStage stage, [NotNull] params IDbAction[] actions)
		{
			if (actions == null) throw new ArgumentNullException(nameof(actions));

			foreach (IDbAction action in actions)
			{
				this.items.Add((stage, action));
			}

			return this;
		}

		public DbActionsBuilder Append<T>(
			DbActionStage stage,
			[NotNull] IEnumerable<T> actions)
			where T : IDbAction
		{
			if (actions == null) throw new ArgumentNullException(nameof(actions));

			this.items.AddRange(actions.Select(a => (stage, (IDbAction) a)));
			return this;
		}

		public DbActionsBuilder Append([NotNull] IEnumerable<DbStep> steps)
		{
			if (steps == null) throw new ArgumentNullException(nameof(steps));
			return steps.Aggregate(this,
				(acc, cur) => acc.Append(
					cur.Stage,
					cur.Actions.Order()));
		}

		public DbActions Build()
		{
			Dictionary<DbActionStage, OrderedItem<IDbAction>[]> stages = this.items
				.WithNaturalOrder()
				.GroupBy(x => x.Value.stage)
				.ToDictionary(gr => gr.Key,
					gr => gr
						.Select(ox => ox.Map(x => x.action))
						.Order()
						.WithNaturalOrder()
						.ToArray());

			OrderedItem<IDbAction>[] Get(DbActionStage stage) =>
				stages.TryGetValue(stage, out OrderedItem<IDbAction>[] acts)
					? acts
					: Array.Empty<OrderedItem<IDbAction>>();

			return new DbActions(
				Get(DbActionStage.PrepareDb),
				Get(DbActionStage.Insert),
				Get(DbActionStage.Delete),
				Get(DbActionStage.CleanupDb));
		}
	}
}