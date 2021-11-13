using System;
using JetBrains.Annotations;
using Reseed.Configuration.Cleanup;

namespace Reseed.Configuration
{
	[PublicAPI]
	public sealed class CleanupOnlySeedMode : AnySeedMode
	{
		public readonly CleanupDefinition CleanupDefinition;

		public CleanupOnlySeedMode([NotNull] CleanupDefinition cleanupDefinition)
		{
			this.CleanupDefinition = cleanupDefinition ?? throw new ArgumentNullException(nameof(cleanupDefinition));
		}
	}
}