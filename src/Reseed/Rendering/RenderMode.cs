using System;
using System.Data.SqlClient;
using JetBrains.Annotations;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Rendering
{
	public abstract class RenderMode
	{
		public readonly CleanupOptions CleanupOptions;

		protected RenderMode([NotNull] CleanupOptions cleanupOptions)
		{
			this.CleanupOptions = cleanupOptions ?? throw new ArgumentNullException(nameof(cleanupOptions));
		}

		public static RenderMode Script([NotNull] CleanupOptions cleanupOptions) =>
			new ScriptMode(cleanupOptions);

		public static RenderMode StoredProcedure(
			[NotNull] ObjectName insertProcedureName,
			[NotNull] ObjectName deleteProcedureName,
			[NotNull] CleanupOptions cleanupOptions) =>
			new StoredProcedureMode(insertProcedureName, deleteProcedureName, cleanupOptions);

		public static RenderMode TempTable(
			[NotNull] string tempSchemaName,
			[NotNull] TempTableInsertMode insertOptions,
			[NotNull] ObjectName deleteProcedureName,
			[NotNull] CleanupOptions cleanupOptions) =>
			new TempTableMode(tempSchemaName, insertOptions, deleteProcedureName, cleanupOptions);
	}

	internal sealed class ScriptMode : RenderMode
	{
		public ScriptMode([NotNull] CleanupOptions cleanupOptions)
			: base(cleanupOptions) { }
	}

	internal sealed class StoredProcedureMode : RenderMode
	{
		public readonly ObjectName InsertProcedureName;
		public readonly ObjectName DeleteProcedureName;

		public StoredProcedureMode(
			[NotNull] ObjectName insertProcedureName,
			[NotNull] ObjectName deleteProcedureName,
			[NotNull] CleanupOptions cleanupOptions)
			: base(cleanupOptions)
		{
			this.InsertProcedureName =
				insertProcedureName ?? throw new ArgumentNullException(nameof(insertProcedureName));
			this.DeleteProcedureName =
				deleteProcedureName ?? throw new ArgumentNullException(nameof(deleteProcedureName));
		}
	}

	internal sealed class TempTableMode : RenderMode
	{
		public readonly string TempSchemaName;
		public readonly TempTableInsertMode InsertOptions;
		public readonly ObjectName DeleteProcedureName;

		public TempTableMode(
			[NotNull] string tempSchemaName,
			[NotNull] TempTableInsertMode insertOptions,
			[NotNull] ObjectName deleteProcedureName,
			[NotNull] CleanupOptions cleanupOptions)
			: base(cleanupOptions)
		{
			if (string.IsNullOrEmpty(tempSchemaName))
				throw new ArgumentException("Value cannot be null or empty.", nameof(tempSchemaName));
			this.TempSchemaName = tempSchemaName;
			this.InsertOptions =
				insertOptions ?? throw new ArgumentNullException(nameof(insertOptions));
			this.DeleteProcedureName =
				deleteProcedureName ?? throw new ArgumentNullException(nameof(deleteProcedureName));
		}
	}

	public abstract class TempTableInsertMode
	{
		public static TempTableInsertMode Script() =>
			new TempTableScriptInsertMode();

		public static TempTableInsertMode Procedure(ObjectName procedureName) =>
			new TempTableProcedureInsertMode(procedureName);

		public static TempTableInsertMode SqlBulkCopy(
			Func<SqlBulkCopyOptions, SqlBulkCopyOptions> customizeOptions = null) =>
			new TempTableSqlBulkCopyInsertMode(customizeOptions);
	}

	internal sealed class TempTableScriptInsertMode : TempTableInsertMode
	{

	}

	internal sealed class TempTableProcedureInsertMode : TempTableInsertMode
	{
		public readonly ObjectName InsertProcedureName;

		public TempTableProcedureInsertMode([NotNull] ObjectName insertProcedureName)
		{
			this.InsertProcedureName =
				insertProcedureName ?? throw new ArgumentNullException(nameof(insertProcedureName));
		}
	}

	internal sealed class TempTableSqlBulkCopyInsertMode : TempTableInsertMode
	{
		public readonly Func<SqlBulkCopyOptions, SqlBulkCopyOptions> CustomizeOptions;

		public TempTableSqlBulkCopyInsertMode(
			Func<SqlBulkCopyOptions, SqlBulkCopyOptions> customizeOptions = null)
		{
			this.CustomizeOptions = customizeOptions ?? Fn.Identity<SqlBulkCopyOptions>();
		}
	}
}