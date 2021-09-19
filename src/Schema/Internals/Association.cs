using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Reseed.Schema.Internals
{
	internal sealed class Association : IEquatable<Association>
	{
		public readonly string Name;
		public readonly Key SourceKey;
		public readonly Key TargetKey;

		public Association([NotNull] string name, [NotNull] Key sourceKey, [NotNull] Key targetKey)
		{
			this.SourceKey = sourceKey ?? throw new ArgumentNullException(nameof(sourceKey));
			this.TargetKey = targetKey ?? throw new ArgumentNullException(nameof(targetKey));
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		public override bool Equals(object obj) => Equals(obj as Association);

		public bool Equals(Association other) =>
			other != null && this.Name.Equals(other.Name, StringComparison.InvariantCultureIgnoreCase);

		public override int GetHashCode() => 
			539060726 + EqualityComparer<string>.Default.GetHashCode(this.Name);

		public override string ToString() => $"({this.SourceKey}) -> ({this.TargetKey}) {this.Name}";
	}
}
