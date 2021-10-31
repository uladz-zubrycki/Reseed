using System;
using Reseed.Generation;

namespace Reseed.Execution
{
	public sealed class SeedActionExecutionException : Exception
	{
		public SeedActionExecutionException(
			ISeedAction action,
			Exception innerException)
			: base(
				$"Error on seed action execution. Action is {action.ToVerboseString()}",
				innerException)
		{
		}
	}
}