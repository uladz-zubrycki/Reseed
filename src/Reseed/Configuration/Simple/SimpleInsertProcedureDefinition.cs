using System;
using JetBrains.Annotations;
using Reseed.Schema;

namespace Reseed.Configuration.Simple
{
	internal sealed class SimpleInsertProcedureDefinition : SimpleInsertDefinition
	{
		public readonly ObjectName ProcedureName;

		public SimpleInsertProcedureDefinition([NotNull] ObjectName procedureName)
		{
			this.ProcedureName = procedureName ?? throw new ArgumentNullException(nameof(procedureName));
		}
	}
}