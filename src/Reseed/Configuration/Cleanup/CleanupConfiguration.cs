using System;
using JetBrains.Annotations;

namespace Reseed.Configuration.Cleanup
{
	internal sealed class CleanupConfiguration
	{
		public readonly CleanupMode Mode;
		public readonly CleanupTarget Target;
		public readonly bool ReseedIdentityColumns;

		public CleanupConfiguration(
			[NotNull] CleanupMode mode, 
			[NotNull] CleanupTarget target,
			bool reseedIdentityColumns = false)
		{
			this.Mode = mode ?? throw new ArgumentNullException(nameof(mode));
			this.Target = target ?? throw new ArgumentNullException(nameof(target));
			this.ReseedIdentityColumns = reseedIdentityColumns;
		}
	}
}