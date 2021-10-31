using System;
using JetBrains.Annotations;
using Reseed.Schema;

namespace Reseed.Configuration.Cleanup
{
	[PublicAPI]
	public abstract class CleanupDefinition
	{
		internal readonly CleanupOptions Options;

		protected CleanupDefinition([NotNull] CleanupOptions options)
		{
			this.Options = options ?? throw new ArgumentNullException(nameof(options));
		}

		public static CleanupDefinition Script([NotNull] CleanupOptions options) =>
			new CleanupScriptDefinition(options);

		public static CleanupDefinition Procedure(
			[NotNull] ObjectName procedureName,
			[NotNull] CleanupOptions cleanupOptions) =>
			new CleanupProcedureDefinition(procedureName, cleanupOptions);
	}

	internal sealed class CleanupScriptDefinition : CleanupDefinition
	{
		public CleanupScriptDefinition([NotNull] CleanupOptions options) : base(options) { }
	}

	internal sealed class CleanupProcedureDefinition : CleanupDefinition
	{
		public readonly ObjectName ProcedureName;

		public CleanupProcedureDefinition(
			[NotNull] ObjectName procedureName, 
			[NotNull] CleanupOptions options) : base(options)
		{
			this.ProcedureName = procedureName ?? throw new ArgumentNullException(nameof(procedureName));
		}
	}
}