using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Data.Providers.FileSystem;

namespace Reseed.Data
{
	public sealed class Entity
	{
		internal readonly EntityOrigin Origin;
		public readonly string Name;
		public readonly IReadOnlyCollection<Property> Properties;

		public Entity(string name, IReadOnlyCollection<Property> properties)
		{
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.Properties = properties ?? throw new ArgumentNullException(nameof(properties));
		}

		internal Entity(
			[NotNull] EntityOrigin origin,
			[NotNull] string name,
			[NotNull] IReadOnlyCollection<Property> properties)
			: this(name, properties)
		{
			this.Origin = origin ?? throw new ArgumentNullException(nameof(origin));
		}

		public override string ToString()
		{
			var props = string.Join("; ", this.Properties.Select(p => p.ToString()));
			return $"{this.Name} {{{props}}}";
		}
	}
}