using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Rendering
{
	public abstract class CleanupMode
	{
		internal readonly DeleteConstraintsResolutionKind ResolutionKind;

		protected CleanupMode(DeleteConstraintsResolutionKind resolutionKind)
		{
			this.ResolutionKind = resolutionKind;
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
			DeleteConstraintsResolutionKind deleteResolutionKind = DeleteConstraintsResolutionKind.OrderTables) => 
			new TruncateCleanupMode(deleteResolutionKind, useDeleteForTables ?? Array.Empty<ObjectName>());

		/// <summary>
		/// Uses TRUNCATE to clean data from tables, which aren't referenced by any foreign key.
		/// Cleans data with use of DELETE FROM otherwise.
		/// </summary>
		public static CleanupMode PreferTruncate(
			ObjectName[] useDeleteForTables = null,
			DeleteConstraintsResolutionKind resolutionKind = DeleteConstraintsResolutionKind.OrderTables) =>
			new PreferTruncateCleanupMode(resolutionKind, useDeleteForTables ?? Array.Empty<ObjectName>());

		/// <summary>
		/// Uses DELETE FROM to clean data from provided tables.
		/// </summary>
		public static CleanupMode Delete(
			DeleteConstraintsResolutionKind resolutionKind = DeleteConstraintsResolutionKind.OrderTables) =>
			new DeleteCleanupMode(resolutionKind);
	}

	public class TruncateCleanupMode : CleanupMode
	{
		private readonly HashSet<ObjectName> useDeleteForTables;

		public TruncateCleanupMode(
			DeleteConstraintsResolutionKind resolutionKind,
			[NotNull] IReadOnlyCollection<ObjectName> useDeleteForTables) 
			: base(resolutionKind)
		{
			if (useDeleteForTables == null) throw new ArgumentNullException(nameof(useDeleteForTables));
			this.useDeleteForTables = useDeleteForTables.ToHashSet();
		}

		internal bool ShouldUseDelete(ObjectName table) => this.useDeleteForTables.Contains(table);
	}

	public sealed class PreferTruncateCleanupMode : TruncateCleanupMode
	{
		public PreferTruncateCleanupMode(
			DeleteConstraintsResolutionKind resolutionKind,
			[NotNull] IReadOnlyCollection<ObjectName> useDeleteForTables) 
			: base(resolutionKind, useDeleteForTables) { }
	}

	public sealed class DeleteCleanupMode : CleanupMode
	{
		public DeleteCleanupMode(DeleteConstraintsResolutionKind resolutionKind) 
			: base(resolutionKind) { }
	}

	public enum DeleteConstraintsResolutionKind
	{
		/// <summary>
		/// Orders tables by their foreign key constraints to be able to execute DELETE FROM.
		/// Temporary disables constraints for mutually dependent tables only. 
		/// </summary>
		OrderTables,

		/// <summary>
		/// Temporary disables all foreign key constraints to be able to execute DELETE FROM.
		/// </summary>
		DisableConstraints
	}
}