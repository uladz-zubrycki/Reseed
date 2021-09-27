using System;
using JetBrains.Annotations;

namespace Reseed.Data
{
	internal sealed class DataFile : IEquatable<DataFile>
	{
		public readonly string Path;
		public readonly string Name;

		public DataFile([NotNull] string path)
		{
			this.Path = path ?? throw new ArgumentNullException(nameof(path));
			this.Name = System.IO.Path.GetFileName(path);
		}

		public override bool Equals(object obj) => Equals(obj as DataFile);

		public bool Equals(DataFile other) =>
			other is not null &&
			(ReferenceEquals(other, this) || Equals(this.Path, other.Path));

		public override int GetHashCode() => this.Path.GetHashCode();
		public override string ToString() => $"'{this.Name}' at '{this.Path}'";
	}
}