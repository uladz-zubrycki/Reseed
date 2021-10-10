using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Graphs;
using Reseed.Ordering;
using Reseed.Rendering.Insertion;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Rendering.TemporaryTables
{
	internal static class TemporaryTableInsertScriptRenderer
	{
		public static DbScript Render(
			[NotNull] OrderedGraph<TableSchema> tables,
			[NotNull] Func<ObjectName, ObjectName> mapTableName)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (mapTableName == null) throw new ArgumentNullException(nameof(mapTableName));

			return new DbScript(
				"Insert from temp tables",
				string.Join(Environment.NewLine + Environment.NewLine,
					MutualReferenceResolver.MergeChunks(
						tables,
						ts =>
							string.Join(Environment.NewLine + Environment.NewLine,
								ts.Order().Select(t =>
									RenderInsertFromTempTables(
										new[] { t },
										Array.Empty<Relation<TableSchema>>(),
										mapTableName))),
						ts =>
							RenderInsertFromTempTables(
								ts.Items.Order(),
								ts.Relations,
								mapTableName)).Order()));
		}

		private static string RenderInsertFromTempTables(
			IEnumerable<TableSchema> tables,
			IReadOnlyCollection<Relation<TableSchema>> foreignKeys,
			Func<ObjectName, ObjectName> mapTableName)
		{
			var fkDecorator = CreateForeignKeysDecorator(foreignKeys);
			return fkDecorator.Decorate(string.Join(Environment.NewLine + Environment.NewLine,
				tables.Select(t =>
				{
					var columnsScript = string.Join(", ", t.Columns
							.Where(c => !c.IsComputed)
							.Select(c => $"[{c.Name}]"))
						.Wrap(100, _ => _, _ => true, ',')
						.WithMargin("\t", '|');

					var identityDecorator = CreateIdentityDecorator(t);
					return identityDecorator.Decorate($@"
						|INSERT INTO {t.Name.GetSqlName()} WITH (TABLOCKX) (
						{columnsScript}
						|)
						|SELECT 
						{columnsScript}
						|FROM {mapTableName(t.Name).GetSqlName()}"
						.TrimMargin('|'));
				})));
		}

		private static IdentityInsertDecorator CreateIdentityDecorator(TableSchema table) =>
			new(
				table.Name,
				table.Columns.Any(c => c.IsIdentity));

		private static DisableForeignKeysDecorator CreateForeignKeysDecorator(
			IReadOnlyCollection<Relation<TableSchema>> foreignKeys) =>
			new(
				foreignKeys
					.Select(r => r.Map(t => t.Name))
					.ToArray());
	}
}