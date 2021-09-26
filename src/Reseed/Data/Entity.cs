using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Reseed.Data
{
	internal sealed class Entity
	{
		public readonly DataFile Origin;
		public readonly string Name;
		public readonly IReadOnlyCollection<Property> Properties;

		public Entity(
			[NotNull] DataFile origin,
			[NotNull] string name,
			[NotNull] IReadOnlyCollection<Property> properties)
		{
			this.Origin = origin ?? throw new ArgumentNullException(nameof(origin));
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.Properties = properties ?? throw new ArgumentNullException(nameof(properties));
		}

		public override string ToString()
		{
			string props = string.Join("; ", this.Properties.Select(p => p.ToString()));
			return $"{this.Name} {{{props}}}";
		}
	}
}