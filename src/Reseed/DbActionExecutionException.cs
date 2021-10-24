using System;
using Reseed.Rendering;

namespace Reseed
{
	public sealed class DbActionExecutionException : Exception
	{
		public DbActionExecutionException(
			IDbAction action,
			Exception innerException)
			: base($"Error on db action execution. Action is {action.ToVerboseString()}", innerException)
		{
		}
	}
}