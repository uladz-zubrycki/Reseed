using System;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Rendering.Schema;

namespace Reseed.Extending.IdentityGeneration
{
	internal sealed class IdentityGeneratorTableExtension: ITableExtension
	{
		public Table Extend([NotNull] Table table)
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			var identityColumns = table.Definition.Columns
				.Where(c => c.IsIdentity)
				.ToArray();

			if (identityColumns.Length == 0)
			{
				return table;
			}
			else
			{
				var identityGenerators = CreateIdentityGenerators(table, identityColumns);
				return table.MapRows(row => identityGenerators.Aggregate(
					row,
					(r, g) => r.GetValue(g.column.Name) == null
						? r.WithValue(g.column.Name, g.generator.NextValue())
						: r));
			}
		}

		private static (Column column, IdentityGenerator generator)[] 
			CreateIdentityGenerators(Table table, Column[] identityColumns) =>
			identityColumns
				.Select(
					c =>
					{
						var existingValues = table.Rows
							.Select(r => int.TryParse(r.Value.GetValue(c.Name), out var n)
								? (int?)n
								: null)
							.Where(n => n != null)
							.Cast<int>()
							.ToArray();

						return (c, new IdentityGenerator(existingValues));
					})
				.ToArray();
	}
}