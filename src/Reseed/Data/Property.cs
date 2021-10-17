using System;
using JetBrains.Annotations;

namespace Reseed.Data
{
	public sealed class Property
	{
		public readonly string Name;
		public readonly string Value;

		public Property([NotNull] string name, [NotNull] string value)
		{
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.Value = value ?? throw new ArgumentNullException(nameof(value));
		}

		public override string ToString() => $"{this.Name} = '{this.Value}'";
	}
}