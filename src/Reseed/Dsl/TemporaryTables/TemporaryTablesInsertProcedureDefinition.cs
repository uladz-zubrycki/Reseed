using System;
using JetBrains.Annotations;
using Reseed.Schema;

namespace Reseed.Dsl.TemporaryTables
{
	internal sealed class TemporaryTablesInsertProcedureDefinition : TemporaryTablesInsertDefinition
	{
		public readonly ObjectName ProcedureName;

		public TemporaryTablesInsertProcedureDefinition([NotNull] ObjectName procedureName)
		{
			this.ProcedureName = procedureName ?? throw new ArgumentNullException(nameof(procedureName));
		}
	}
}