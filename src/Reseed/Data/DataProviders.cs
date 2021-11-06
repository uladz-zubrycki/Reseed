using System;
using JetBrains.Annotations;
using Reseed.Data.Providers.FileSystem;

namespace Reseed.Data
{
	public static class DataProviders
	{
		public static IDataProvider Xml([NotNull] string dataFolder) =>
			Xml(dataFolder, _ => true);

		public static IDataProvider Xml(
			[NotNull] string dataFolder,
			[NotNull] Func<string, bool> filter) =>
			Xml(dataFolder, "*.xml", filter);

		public static IDataProvider Xml(
			[NotNull] string dataFolder,
			[NotNull] string filePattern,
			[NotNull] Func<string, bool> fileFilter) =>
			new XmlDataProvider(dataFolder, filePattern, fileFilter);
	}
}
