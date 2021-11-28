using System;
using System.IO;
using JetBrains.Annotations;

namespace Reseed.Data.Providers.FileSystem
{
	internal sealed class DataFile : EntityOrigin
	{
		public readonly string FilePath;
		public readonly string FileName;
		public readonly string Directory;

		public override string OriginName => $"'{this.FileName}' in '{this.Directory}'";

		public DataFile([NotNull] string path)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			var fullPath = Path.GetFullPath(path);
			this.FilePath = fullPath;
			this.FileName = Path.GetFileName(fullPath);
			this.Directory = Path.GetDirectoryName(fullPath);
		}
	}
}