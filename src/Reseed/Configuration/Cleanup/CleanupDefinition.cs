using System;
using JetBrains.Annotations;
using Reseed.Schema;

namespace Reseed.Configuration.Cleanup
{
	[PublicAPI]
	public abstract class AnyCleanupDefinition
	{
	}

	[PublicAPI]
	public abstract class CleanupDefinition : AnyCleanupDefinition
	{
		public static EmptyCleanupDefinition NoCleanup() => EmptyCleanupDefinition.Instance;

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

	[PublicAPI]
	public sealed class EmptyCleanupDefinition : AnyCleanupDefinition
	{
		public static readonly EmptyCleanupDefinition Instance = new();
	}

	internal sealed class CleanupScriptDefinition : CleanupDefinition
	{
		internal readonly CleanupConfiguration Configuration;

		public CleanupScriptDefinition([NotNull] CleanupConfiguration configuration)
		{
			this.Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		}
	}

	internal sealed class CleanupProcedureDefinition : CleanupDefinition
	{
		public readonly ObjectName ProcedureName;
		internal readonly CleanupConfiguration Configuration;

		public CleanupProcedureDefinition(
			[NotNull] ObjectName procedureName,
			[NotNull] CleanupConfiguration configuration)
		{
			this.ProcedureName = procedureName ?? throw new ArgumentNullException(nameof(procedureName));
			this.Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		}
	}
}