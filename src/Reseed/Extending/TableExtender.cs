using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Extending.IdentityGeneration;
using Reseed.Generation.Schema;

namespace Reseed.Extending
{
	internal static class TableExtender
	{
		private static readonly ITableExtension[] DefaultExtensions =
		{
			new IdentityGeneratorTableExtension()
		};

		public static IReadOnlyCollection<Table> Extend([NotNull] IReadOnlyCollection<Table> tables)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			return tables
				.Select(t => DefaultExtensions.Aggregate(t, (acc, cur) => cur.Extend(acc)))
				.ToArray();
		}
	}

	public interface ITableExtension
	{
		Table Extend(Table table);
	}
}