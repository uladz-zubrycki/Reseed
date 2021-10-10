using System;
using System.Data.SqlClient;
using Reseed.Utils;

namespace Reseed.Dsl.TemporaryTables
{
	internal sealed class TemporaryTablesInsertSqlBulkCopyDefinition : TemporaryTablesInsertDefinition
	{
		public readonly Func<SqlBulkCopyOptions, SqlBulkCopyOptions> CustomizeBulkCopy;

		public TemporaryTablesInsertSqlBulkCopyDefinition(
			Func<SqlBulkCopyOptions, SqlBulkCopyOptions> customizeBulkCopy = null)
		{
			this.CustomizeBulkCopy = customizeBulkCopy ?? Fn.Identity<SqlBulkCopyOptions>();
		}
	}
}