using System;
using JetBrains.Annotations;
using Reseed.Schema;

namespace Reseed.Configuration.Cleanup
{
	[PublicAPI]
	public abstract class CleanupDefinition
	{
		internal readonly CleanupConfiguration Configuration;

		internal CleanupDefinition([NotNull] CleanupConfiguration configuration)
		{
			this.Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		}

		public static CleanupDefinition Script(
			[NotNull] CleanupMode mode,
			[NotNull] CleanupTarget target) =>
			new CleanupScriptDefinition(new CleanupConfiguration(mode, target));

		public static CleanupDefinition Procedure(
			[NotNull] ObjectName procedureName,
			[NotNull] CleanupMode mode,
			[NotNull] CleanupTarget target) =>
			new CleanupProcedureDefinition(
				procedureName, 
				new CleanupConfiguration(mode, target));
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