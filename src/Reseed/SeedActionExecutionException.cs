﻿using System;
using Reseed.Rendering;

namespace Reseed
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