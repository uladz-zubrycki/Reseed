using System;
using System.Data.SqlClient;
using Reseed.Internals.Utils;

namespace Reseed.Configuration.TemporaryTables
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