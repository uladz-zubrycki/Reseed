using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Generation.Cleanup
{
	internal static class IdentityReseedScriptRenderer
	{
		public static SqlScriptAction Render([NotNull] IReadOnlyCollection<TableSchema> tables)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			return new SqlScriptAction("Reseed identity columns",
				tables
					.Select(RenderTableScript)
					.JoinStrings(Environment.NewLine));
		}

		private static string RenderTableScript(TableSchema table)
		{
			var identityColumn = table.Columns.FirstOrDefault(c => c.IsIdentity);
			if (identityColumn == null)
			{
				throw new ArgumentException(
					$"Can't reseed '{table.Name}' table, it has no identity columns");
			}

			return $@"
				|DBCC CHECKIDENT
				|(
				|	'{table.Name.GetSqlName()}', RESEED, {identityColumn.IdentitySeed}
				|);".TrimMargin('|');
		}
	}
}
