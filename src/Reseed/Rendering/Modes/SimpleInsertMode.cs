using System;
using JetBrains.Annotations;
using Reseed.Schema;

namespace Reseed.Rendering.Modes
{
	[PublicAPI]
	public abstract class SimpleInsertMode
	{
		public static SimpleInsertMode Script() =>
			new SimpleInsertScriptMode();

		public static SimpleInsertMode Procedure([NotNull] ObjectName procedureName) =>
			new SimpleInsertProcedureMode(procedureName);
	}

	internal sealed class SimpleInsertScriptMode : SimpleInsertMode { }

	internal sealed class SimpleInsertProcedureMode : SimpleInsertMode
	{
		public readonly ObjectName ProcedureName;

		public SimpleInsertProcedureMode([NotNull] ObjectName procedureName)
		{
			this.ProcedureName = procedureName ?? throw new ArgumentNullException(nameof(procedureName));
		}
	}
}