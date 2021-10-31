using JetBrains.Annotations;

namespace Reseed.Configuration.Cleanup
{
	[PublicAPI]
	public enum ConstraintResolutionBehavior
	{
		/// <summary>
		/// Orders tables by their foreign key constraints to be able to execute DELETE FROM.
		/// Temporary disables constraints for mutually dependent tables only. 
		/// </summary>
		OrderTables,

		/// <summary>
		/// Temporary disables all foreign key constraints to be able to execute DELETE FROM.
		/// </summary>
		DisableConstraints
	}
}