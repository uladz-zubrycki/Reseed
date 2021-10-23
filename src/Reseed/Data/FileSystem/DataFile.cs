using System;
using JetBrains.Annotations;

namespace Reseed.Data.FileSystem
{
	internal sealed class DataFile : IEquatable<DataFile>
	{
		public readonly string Path;
		public readonly string Name;
		public readonly string Directory;

		public DataFile([NotNull] string path)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			var fullPath = System.IO.Path.GetFullPath(path);
			this.Path = fullPath;
			this.Name = System.IO.Path.GetFileName(fullPath);
			this.Directory = System.IO.Path.GetDirectoryName(fullPath);
		}

		public override bool Equals(object obj) => Equals(obj as DataFile);

		public bool Equals(DataFile other) =>
			other is not null &&
			(ReferenceEquals(other, this) || Equals(this.Path, other.Path));

		public override int GetHashCode() => this.Path.GetHashCode();
		public override string ToString() => $"'{this.Name}' in '{this.Directory}'";
	}
}