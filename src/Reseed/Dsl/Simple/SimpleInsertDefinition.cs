using JetBrains.Annotations;
using Reseed.Schema;

namespace Reseed.Dsl.Simple
{
	[PublicAPI]
	public abstract class SimpleInsertDefinition
	{
		public static SimpleInsertDefinition Script() =>
			new SimpleInsertScriptDefinition();

		public static SimpleInsertDefinition Procedure([NotNull] ObjectName procedureName) =>
			new SimpleInsertProcedureDefinition(procedureName);
	}
}