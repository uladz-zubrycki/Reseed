using System;
using JetBrains.Annotations;
using Reseed.Schema;

namespace Reseed.Rendering.Modes
{
	[PublicAPI]
	public abstract class CleanupMode
	{
		internal readonly CleanupOptions Options;

		protected CleanupMode([NotNull] CleanupOptions options)
		{
			this.Options = options ?? throw new ArgumentNullException(nameof(options));
		}

		public static CleanupMode Script([NotNull] CleanupOptions options) =>
			new CleanupScriptMode(options);

		public static CleanupMode Procedure(
			[NotNull] ObjectName procedureName,
			[NotNull] CleanupOptions cleanupOptions) =>
			new CleanupProcedureMode(procedureName, cleanupOptions);
	}

	internal sealed class CleanupScriptMode : CleanupMode
	{
		public CleanupScriptMode([NotNull] CleanupOptions options) : base(options) { }
	}

	internal sealed class CleanupProcedureMode : CleanupMode
	{
		public readonly ObjectName ProcedureName;

		public CleanupProcedureMode(
			[NotNull] ObjectName procedureName, 
			[NotNull] CleanupOptions options) : base(options)
		{
			this.ProcedureName = procedureName ?? throw new ArgumentNullException(nameof(procedureName));
		}
	}
}