using System;
using JetBrains.Annotations;
using Reseed.Schema;

namespace Reseed.Configuration.Basic
{
	internal sealed class BasicInsertProcedureDefinition : BasicInsertDefinition
	{
		public readonly ObjectName ProcedureName;

		public BasicInsertProcedureDefinition([NotNull] ObjectName procedureName)
		{
			this.ProcedureName = procedureName ?? throw new ArgumentNullException(nameof(procedureName));
		}
	}
}