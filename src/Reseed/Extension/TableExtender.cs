using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Extension.IdentityGeneration;
using Reseed.Generation.Schema;

namespace Reseed.Extension
{
	internal static class TableExtender
	{
		public static IReadOnlyCollection<Table> Extend(
			[NotNull] IReadOnlyCollection<Table> tables,
			[NotNull] DataExtensionOptions options)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (options == null) throw new ArgumentNullException(nameof(options));

			var extensions = GetExtensions(options);
			return extensions.Count == 0
				? tables
				: tables
					.Select(t => extensions.Aggregate(t, (acc, cur) => cur.Extend(acc)))
					.ToArray();
		}

		private static IReadOnlyCollection<ITableExtension> GetExtensions(
			DataExtensionOptions options)
		{
			var extensions = new List<ITableExtension>();

			if (options.GenerateIdentityValues)
			{
				extensions.Add(IdentityGeneratorTableExtension.Instance);
			}

			return extensions.ToArray();
		}
	}

	public interface ITableExtension
	{
		Table Extend(Table table);
	}
}