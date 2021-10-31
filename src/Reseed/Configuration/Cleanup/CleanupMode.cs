using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Internals.Utils;
using Reseed.Schema;

namespace Reseed.Configuration.Cleanup
{
	[PublicAPI]
	public abstract class CleanupMode
	{
		internal readonly ConstraintResolutionBehavior ConstraintBehavior;

		protected CleanupMode(ConstraintResolutionBehavior constraintBehavior)
		{
			this.ConstraintBehavior = constraintBehavior;
		}

		/// <summary>
		/// Uses TRUNCATE to clean data from provided tables.
		/// In order to do so it drops foreign keys referencing tables and recreates them afterwards.
		/// Note that truncate is not possible if table is referenced by a view with index.
		/// This case isn't handled automatically, therefore script will just fail if such table exists.
		/// To fix this either drop required views/indexes manually or choose delete mode for these tables.
		/// </summary>
		public static CleanupMode Truncate(
			ObjectName[] useDeleteForTables = null,
			ConstraintResolutionBehavior constraintBehavior = ConstraintResolutionBehavior.OrderTables) => 
			new TruncateCleanupMode(constraintBehavior, useDeleteForTables ?? Array.Empty<ObjectName>());

		/// <summary>
		/// Uses TRUNCATE to clean data from tables, which aren't referenced by any foreign key.
		/// Cleans data with use of DELETE FROM otherwise.
		/// </summary>
		public static CleanupMode PreferTruncate(
			ObjectName[] useDeleteForTables = null,
			ConstraintResolutionBehavior constraintBehavior = ConstraintResolutionBehavior.OrderTables) =>
			new PreferTruncateCleanupMode(constraintBehavior, useDeleteForTables ?? Array.Empty<ObjectName>());

		/// <summary>
		/// Uses DELETE FROM to clean data from provided tables.
		/// </summary>
		public static CleanupMode Delete(
			ConstraintResolutionBehavior constraintBehavior = ConstraintResolutionBehavior.OrderTables) =>
			new DeleteCleanupMode(constraintBehavior);
	}

	internal sealed class DeleteCleanupMode : CleanupMode
	{
		public DeleteCleanupMode(ConstraintResolutionBehavior constraintBehavior) 
			: base(constraintBehavior) { }
	}

	internal sealed class PreferTruncateCleanupMode : TruncateCleanupMode
	{
		public PreferTruncateCleanupMode(
			ConstraintResolutionBehavior constraintBehavior,
			[NotNull] IReadOnlyCollection<ObjectName> useDeleteForTables) 
			: base(constraintBehavior, useDeleteForTables) { }
	}

	internal class TruncateCleanupMode : CleanupMode
	{
		private readonly HashSet<ObjectName> useDeleteForTables;

		public TruncateCleanupMode(
			ConstraintResolutionBehavior constraintBehavior,
			[NotNull] IReadOnlyCollection<ObjectName> useDeleteForTables) 
			: base(constraintBehavior)
		{
			if (useDeleteForTables == null) throw new ArgumentNullException(nameof(useDeleteForTables));
			this.useDeleteForTables = useDeleteForTables.ToHashSet();
		}

		internal bool ShouldUseDelete(ObjectName table) => this.useDeleteForTables.Contains(table);
	}
}