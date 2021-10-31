using System;
using JetBrains.Annotations;
using Reseed.Schema;

namespace Reseed.Configuration.Cleanup
{
	[PublicAPI]
	public abstract class CleanupDefinition
	{
		internal readonly CleanupConfiguration Configuration;

		protected CleanupDefinition([NotNull] CleanupConfiguration configuration)
		{
			this.Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		}

		public static CleanupDefinition Script([NotNull] CleanupConfiguration configuration) =>
			new CleanupScriptDefinition(configuration);

		public static CleanupDefinition Procedure(
			[NotNull] ObjectName procedureName,
			[NotNull] CleanupConfiguration configuration) =>
			new CleanupProcedureDefinition(procedureName, configuration);
	}

	internal sealed class CleanupScriptDefinition : CleanupDefinition
	{
		public CleanupScriptDefinition([NotNull] CleanupConfiguration configuration) 
			: base(configuration) { }
	}

	internal sealed class CleanupProcedureDefinition : CleanupDefinition
	{
		public readonly ObjectName ProcedureName;

		public CleanupProcedureDefinition(
			[NotNull] ObjectName procedureName, 
			[NotNull] CleanupConfiguration configuration) : base(configuration)
		{
			this.ProcedureName = procedureName ?? throw new ArgumentNullException(nameof(procedureName));
		}
	}
}