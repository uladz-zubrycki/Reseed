using System;
using System.Collections.Generic;
using System.Globalization;
using JetBrains.Annotations;
using Reseed.Generation.Schema;
using Reseed.Utils;

namespace Reseed.Extension.IdentityGeneration
{
	internal sealed class IdentityGenerator
	{
		private readonly HashSet<decimal> excludedValues;
		private readonly decimal increment;
		private decimal current;

		public IdentityGenerator(
			[NotNull] IdentityOptions identity,
			[NotNull] IReadOnlyCollection<decimal> excludedValues)
		{
			if (identity == null) throw new ArgumentNullException(nameof(identity));
			if (excludedValues == null) throw new ArgumentNullException(nameof(excludedValues));
			this.excludedValues = excludedValues.ToHashSet();
			this.increment = identity.Increment;
			this.current = identity.Seed;
		}

		public string NextValue()
		{
			while (true)
			{
				this.current += this.increment;
				if (!this.excludedValues.Contains(this.current))
				{
					return this.current.ToString(CultureInfo.InvariantCulture);
				}
			}
		}
	}
}