using System;
using JetBrains.Annotations;

namespace Reseed.Configuration.Cleanup
{
	internal sealed class CleanupConfiguration
	{
		public readonly CleanupMode Mode;
		public readonly CleanupTarget Target;

		public CleanupConfiguration([NotNull] CleanupMode mode, [NotNull] CleanupTarget target)
		{
			this.Mode = mode ?? throw new ArgumentNullException(nameof(mode));
			this.Target = target ?? throw new ArgumentNullException(nameof(target));
		}
	}
}