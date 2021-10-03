using System;
using System.Data.SqlClient;
using JetBrains.Annotations;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Rendering.Modes
{
	[PublicAPI]
	public abstract class TemporaryTablesInsertMode
	{
		public static TemporaryTablesInsertMode Script() =>
			new TemporaryTablesInsertScriptMode();

		public static TemporaryTablesInsertMode Procedure(ObjectName procedureName) =>
			new TemporaryTablesInsertProcedureMode(procedureName);

		public static TemporaryTablesInsertMode SqlBulkCopy(
			Func<SqlBulkCopyOptions, SqlBulkCopyOptions> customizeBulkCopy = null) =>
			new TemporaryTablesInsertSqlBulkCopyMode(customizeBulkCopy);
	}

	internal sealed class TemporaryTablesInsertScriptMode : TemporaryTablesInsertMode { }

	internal sealed class TemporaryTablesInsertProcedureMode : TemporaryTablesInsertMode
	{
		public readonly ObjectName ProcedureName;

		public TemporaryTablesInsertProcedureMode([NotNull] ObjectName procedureName)
		{
			this.ProcedureName = procedureName ?? throw new ArgumentNullException(nameof(procedureName));
		}
	}

	internal sealed class TemporaryTablesInsertSqlBulkCopyMode : TemporaryTablesInsertMode
	{
		public readonly Func<SqlBulkCopyOptions, SqlBulkCopyOptions> CustomizeBulkCopy;

		public TemporaryTablesInsertSqlBulkCopyMode(
			Func<SqlBulkCopyOptions, SqlBulkCopyOptions> customizeBulkCopy = null)
		{
			this.CustomizeBulkCopy = customizeBulkCopy ?? Fn.Identity<SqlBulkCopyOptions>();
		}
	}
}