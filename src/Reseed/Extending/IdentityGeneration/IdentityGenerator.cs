using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Utils;

namespace Reseed.Extending.IdentityGeneration
{
	internal sealed class IdentityGenerator
	{
		// TODO: Use Identity column seed and step
		private int current;
		private readonly HashSet<int> excludedValues;

		public IdentityGenerator([NotNull] IReadOnlyCollection<int> excludedValues)
		{
			if (excludedValues == null) throw new ArgumentNullException(nameof(excludedValues));
			this.excludedValues = excludedValues.ToHashSet();
		}

		public string NextValue()
		{
			while (true)
			{
				this.current++;
				if (!this.excludedValues.Contains(this.current))
				{
					return this.current.ToString();
				}
			}
		}
	}
}