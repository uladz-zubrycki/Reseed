using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Utils;

namespace Reseed.Rendering
{
	internal sealed class IdentityGenerator
	{
		private readonly HashSet<int> excludedValues;
		private int current = 1;

		public IdentityGenerator([NotNull] IReadOnlyCollection<int> excludedValues)
		{
			if (excludedValues == null) throw new ArgumentNullException(nameof(excludedValues));
			this.excludedValues = excludedValues.ToHashSet();
		}

		public string NextValue()
		{
			var result = this.current;

			while (true)
			{
				this.current++;
				if (!this.excludedValues.Contains(this.current))
				{
					break;
				}
			}

			return result.ToString();
		}
	}
}