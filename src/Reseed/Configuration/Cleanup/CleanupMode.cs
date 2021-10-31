using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Configuration.Cleanup
{
	[PublicAPI]
	public abstract class CleanupMode
	{
		internal readonly ConstraintsResolutionKind ConstraintsResolution;

		protected CleanupMode(ConstraintsResolutionKind constraintsResolution)
		{
			this.ConstraintsResolution = constraintsResolution;
		}

		/// <summary>
		/// Uses TRUNCATE to clean data from provided tables.
		/// In order to do so it drops foreign keys referencing tables and recreates them afterwards.
		/// Note that truncate is not possible if table is referenced by a view with index.
		/// This case isn't handled automatically, therefore script will just fail if such table exists.
		/// To fix this either drop required views/indexes manually or choose delete mode for these tables.
		/// </summary>
		public static CleanupMode Truncate(
			ObjectName[] forceDeleteForTables = null,
			ConstraintsResolutionKind resolutionKind = ConstraintsResolutionKind.OrderTables) => 
			new TruncateCleanupMode(resolutionKind, forceDeleteForTables ?? Array.Empty<ObjectName>());

		/// <summary>
		/// Uses TRUNCATE to clean data from tables, which aren't referenced by any foreign key.
		/// Cleans data with use of DELETE FROM otherwise.
		/// </summary>
		public static CleanupMode PreferTruncate(
			ObjectName[] forceDeleteForTables = null,
			ConstraintsResolutionKind resolutionKind = ConstraintsResolutionKind.OrderTables) =>
			new PreferTruncateCleanupMode(resolutionKind, forceDeleteForTables ?? Array.Empty<ObjectName>());

		/// <summary>
		/// Uses DELETE FROM to clean data from provided tables.
		/// </summary>
		public static CleanupMode Delete(
			ConstraintsResolutionKind resolutionKind = ConstraintsResolutionKind.OrderTables) =>
			new DeleteCleanupMode(resolutionKind);
	}

	internal sealed class DeleteCleanupMode : CleanupMode
	{
		public DeleteCleanupMode(ConstraintsResolutionKind constraintsResolution) 
			: base(constraintsResolution) { }
	}

	internal sealed class PreferTruncateCleanupMode : TruncateCleanupMode
	{
		public PreferTruncateCleanupMode(
			ConstraintsResolutionKind constraintsResolution,
			[NotNull] IReadOnlyCollection<ObjectName> useDeleteForTables) 
			: base(constraintsResolution, useDeleteForTables) { }
	}

	internal class TruncateCleanupMode : CleanupMode
	{
		private readonly HashSet<ObjectName> useDeleteForTables;

		public TruncateCleanupMode(
			ConstraintsResolutionKind constraintsResolution,
			[NotNull] IReadOnlyCollection<ObjectName> useDeleteForTables) 
			: base(constraintsResolution)
		{
			if (useDeleteForTables == null) throw new ArgumentNullException(nameof(useDeleteForTables));
			this.useDeleteForTables = useDeleteForTables.ToHashSet();
		}

		internal bool ShouldUseDelete(ObjectName table) => this.useDeleteForTables.Contains(table);
	}
}