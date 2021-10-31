using System;
using System.Data.SqlClient;
using JetBrains.Annotations;
using Reseed.Schema;

namespace Reseed.Configuration.TemporaryTables
{
	[PublicAPI]
	public abstract class TemporaryTablesInsertDefinition
	{
		public static TemporaryTablesInsertDefinition Script() =>
			new TemporaryTablesInsertScriptDefinition();

		public static TemporaryTablesInsertDefinition Procedure(ObjectName procedureName) =>
			new TemporaryTablesInsertProcedureDefinition(procedureName);

		public static TemporaryTablesInsertDefinition SqlBulkCopy(
			Func<SqlBulkCopyOptions, SqlBulkCopyOptions> customizeBulkCopy = null) =>
			new TemporaryTablesInsertSqlBulkCopyDefinition(customizeBulkCopy);
	}
}