using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using JetBrains.Annotations;
using Reseed.Configuration;
using Reseed.Generation;
using Reseed.Ordering;

namespace Reseed;

public static class ReseederExtensions
{
	public static SeedActions Generate(
		[NotNull] this Reseeder reseeder,
		[NotNull] string connectionString,
		[NotNull] AnySeedMode mode)
	{
		if (reseeder == null) throw new ArgumentNullException(nameof(reseeder));
		using var connection = new SqlConnection(connectionString);
		connection.Open();
		return reseeder.Generate(connection, mode);
	}

	public static void Execute(
		[NotNull] this Reseeder reseeder,
		[NotNull] string connectionString,
		[NotNull] IReadOnlyCollection<OrderedItem<ISeedAction>> actions,
		TimeSpan? actionTimeout = null)
	{
		if (reseeder == null) throw new ArgumentNullException(nameof(reseeder));
		if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
		if (actions == null) throw new ArgumentNullException(nameof(actions));
		if (actions.Count == 0)
		{
			return;
		}

		using var connection = new SqlConnection(connectionString);
		connection.Open();
		reseeder.Execute(connection, actions, actionTimeout);
	}
}