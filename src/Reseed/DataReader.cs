using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;
using Reseed.Data;

namespace Reseed
{
	internal static class DataReader
	{
		public static IReadOnlyCollection<Entity> LoadData([NotNull] string root)
		{
			if (root == null) throw new ArgumentNullException(nameof(root));
			if (!Directory.Exists(root))
			{
				throw new InvalidOperationException(
					$"Can't find test data files at '{root}', specified directory doesn't exist");
			}

			string[] files = Directory
				.EnumerateFiles(root, "*.xml", SearchOption.AllDirectories)
				.ToArray();

			if (!files.Any())
			{
				throw new InvalidOperationException(
					$"At least one test data file is required, but found none at '{root}'");
			}

			return files
				.SelectMany(ReadFile)
				.ToArray();
		}

		private static Entity[] ReadFile([NotNull] string path)
		{
			const string rootName = "Crexi-Main";
			var file = new DataFile(path);
			
			XElement root = XDocument.Load(path).Elements().SingleOrDefault();
			// TODO: Enable after cleaning namespaces from test data
			//if (root?.Name != XName.Get(rootName))
			//{
			//	string rootIssue = root == null ? "no element" : $"element with name '{root.Name}'";

			//	throw BuildDocumentError(file,
			//		$"Expected to have the only root element with name '{rootName}' and no namespace, " +
			//		$"but got {rootIssue}");
			//}

			return root.Elements()
				.Select(e => ParseEntity(file, e))
				.ToArray();
		}

		private static Entity ParseEntity(DataFile origin, XElement element)
		{
			AssertNoAttributes(origin, element);

			XElement[] descendants = element.Elements().ToArray();
			if (descendants.Length == 0)
			{
				throw BuildDocumentError(
					origin,
					$"At least one descendant node is required, but found empty element '{element.Name}'");
			}

			return new Entity(
				origin,
				element.Name.LocalName,
				descendants.Select(e => ParseProperty(origin, e)).ToArray());
		}

		private static Property ParseProperty(DataFile origin, XElement element)
		{
			AssertNoAttributes(origin, element);
			AssertNoDescendants(origin, element);
			return new Property(element.Name.LocalName, element.Value);
		}

		private static void AssertNoDescendants(DataFile dataFile, XElement element)
		{
			if (element.Descendants().Any())
			{
				throw BuildDocumentError(
					dataFile,
					"Expected to have flat document structure, " +
					$"but found descendant nodes at property element '{element.Name}'");
			}
		}

		private static void AssertNoAttributes(DataFile origin, XElement element)
		{
			if (element.Attributes().Any())
			{
				throw BuildDocumentError(
					origin,
					$"Attributes aren't supported, but found at element '{element.Name}'");
			}
		}

		private static Exception BuildDocumentError(DataFile origin, string error) =>
			new InvalidOperationException(
				$"Can't process test data document '{origin.Name}' at '{origin.Path}'. " +
				error);
	}
}