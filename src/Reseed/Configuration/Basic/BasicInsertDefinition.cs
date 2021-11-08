using JetBrains.Annotations;
using Reseed.Schema;

namespace Reseed.Configuration.Basic
{
	[PublicAPI]
	public abstract class BasicInsertDefinition
	{
		public static BasicInsertDefinition Script() =>
			new BasicInsertScriptDefinition();

		public static BasicInsertDefinition Procedure([NotNull] ObjectName procedureName) =>
			new BasicInsertProcedureDefinition(procedureName);
	}
}