using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Rendering
{
	public abstract class DataCleanupMode
	{
		public readonly DeleteConstraintsResolutionKind DeleteResolutionKind;

		protected DataCleanupMode(DeleteConstraintsResolutionKind deleteResolutionKind)
		{
			this.DeleteResolutionKind = deleteResolutionKind;
		}

		/// <summary>
		/// Uses TRUNCATE to clean data from provided tables.
		/// In order to do so it drops foreign keys referencing tables and recreates them afterwards.
		/// Note that truncate is not possible if table is referenced by a view with index.
		/// This case isn't handled automatically, therefore script will just fail if such table exists.
		/// To fix this either drop required views/indexes manually or choose delete mode for these tables.
		/// </summary>
		public static DataCleanupMode Truncate(
			ObjectName[] useDeleteForTables = null,
			DeleteConstraintsResolutionKind deleteResolutionKind = DeleteConstraintsResolutionKind.OrderTables) => 
			new TruncateDataCleanupMode(deleteResolutionKind, useDeleteForTables ?? Array.Empty<ObjectName>());

		/// <summary>
		/// Uses TRUNCATE to clean data from tables, which aren't referenced by any foreign key.
		/// Cleans data with use of DELETE FROM otherwise.
		/// </summary>
		public static DataCleanupMode PreferTruncate(
			ObjectName[] useDeleteForTables = null,
			DeleteConstraintsResolutionKind deleteResolutionKind = DeleteConstraintsResolutionKind.OrderTables) =>
			new PreferTruncateDataCleanupMode(deleteResolutionKind, useDeleteForTables ?? Array.Empty<ObjectName>());

		/// <summary>
		/// Uses DELETE FROM to clean data from provided tables.
		/// </summary>
		public static DataCleanupMode Delete(
			DeleteConstraintsResolutionKind deleteResolutionKind = DeleteConstraintsResolutionKind.OrderTables) =>
			new DeleteDataCleanupMode(deleteResolutionKind);
	}

	public class TruncateDataCleanupMode : DataCleanupMode
	{
		private readonly HashSet<ObjectName> useDeleteForTables;

		public TruncateDataCleanupMode(
			DeleteConstraintsResolutionKind deleteResolutionKind,
			[NotNull] IReadOnlyCollection<ObjectName> useDeleteForTables) 
			: base(deleteResolutionKind)
		{
			if (useDeleteForTables == null) throw new ArgumentNullException(nameof(useDeleteForTables));
			this.useDeleteForTables = useDeleteForTables.ToHashSet();
		}

		public bool ShouldUseDelete(ObjectName table) => this.useDeleteForTables.Contains(table);
	}

	public sealed class PreferTruncateDataCleanupMode : TruncateDataCleanupMode
	{
		public PreferTruncateDataCleanupMode(
			DeleteConstraintsResolutionKind deleteResolutionKind,
			[NotNull] IReadOnlyCollection<ObjectName> useDeleteForTables) 
			: base(deleteResolutionKind, useDeleteForTables) { }
	}

	public sealed class DeleteDataCleanupMode : DataCleanupMode
	{
		public DeleteDataCleanupMode(DeleteConstraintsResolutionKind deleteResolutionKind) 
			: base(deleteResolutionKind) { }
	}

	public enum DeleteConstraintsResolutionKind
	{
		/// <summary>
		/// Orders tables by their foreign key constraints to be able to execute DELETE FROM.
		/// Temporary disables constraints for mutually dependent tables only. 
		/// </summary>
		OrderTables,

		/// <summary>
		/// Temporary disables all foreign key constraints to be able to execute DELETE FROM
		/// </summary>
		DisableConstraints
	}
}