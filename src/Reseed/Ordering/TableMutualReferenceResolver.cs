using System;
using System.Collections.Generic;
using System.Linq;
using Reseed.Generation.Schema;
using Reseed.Graphs;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Ordering
{
	internal static class TableMutualReferenceResolver
	{
		public static IReadOnlyCollection<OrderedItem<ITableContainer>> Resolve(OrderedGraph<Table> tables)
		{
			var getTableRelations = BuildRelationsGetter(tables.MutualReferences);
			return MutualReferenceResolver.MergeChunks(
					tables,
					ts => ts
						.Select(o => o.Map(t => (ITableContainer) t))
						.ToArray(),
					ts => OrderMutualTables(ts, getTableRelations))
				.Flatten();
		}

		private static OrderedItem<ITableContainer>[] OrderMutualTables(
			MutualGroup<Table> tables,
			Func<Table, Relation<Table>[]> getTableRelations)
		{
			var tableGroup = tables.Items.Select(o => o.Value).ToArray();
			var rows = CollectTableRows(tableGroup, getTableRelations);
			var orderedRows = NodeOrderer<TableRow>.Order(rows);

			return MutualReferenceResolver.MergeChunks(
				orderedRows,
				rs => (ITableContainer) CreateTableGroup(tableGroup, rs),
				rs => CreateRowGroup(tableGroup, rs));
		}

		private static MutualTableGroup CreateTableGroup(
			Table[] tables,
			IReadOnlyCollection<OrderedItem<TableRow>> rows) =>
			new(
				tables.Select(t => t.Definition).ToArray(),
				rows.Select(o => o.Map(tr => tr.Row))
					.ToArray());

		private static MutualRowGroup CreateRowGroup(
			Table[] tables,
			MutualGroup<TableRow> group)
		{
			var tableDefinitions = tables.Select(t => t.Definition).ToArray();

			var rows = group.Items
				.Select(o => o.Map(r => r.Row))
				.ToArray();

			var relations =
				group.Relations
					.Select(r => r.Map(t => t.Table.Name))
					.Distinct()
					.ToArray();

			return new MutualRowGroup(
				tableDefinitions,
				rows,
				relations);
		}

		private static IReadOnlyCollection<TableRow> CollectTableRows(
			Table[] tables,
			Func<Table, Relation<Table>[]> getTableRelations)
		{
			var getRows = BuildRowGetter(tables, getTableRelations);
			var rows = tables.SelectMany(t => t.Rows.Select(r => r.Value)).ToArray();
			var rowRelations = CollectRowRelations(tables, getTableRelations, getRows);
			var tableDefinitionMap =
				tables.ToDictionary(x => x.Name, x => x.Definition);

			return NodeBuilder<TableRow>.CollectNodes(
				rows,
				rowRelations,
				(r, tr) => r.Map(_ => tr),
				(row, references) => new TableRow(
					row,
					tableDefinitionMap[row.Table.Name],
					references));
		}

		private static Relation<Row>[] CollectRowRelations(
			Table[] tables,
			Func<Table, Relation<Table>[]> getTableRelations,
			Func<Table, Key, (Row row, KeyValue value)[]> getTableRows) =>
			tables.SelectMany(t => getTableRelations(t).SelectMany(relation =>
				{
					(Row, KeyValue)[] keyedSourceRows =
						getTableRows(t, relation.Association.SourceKey)
							.Where(x => x.value.HasValue)
							.ToArray();

					return JoinRows(
						keyedSourceRows,
						getTableRows(relation.Target, relation.Association.TargetKey),
						relation.Association);
				}))
				.ToArray();

		private static Relation<Row>[] JoinRows(
			(Row row, KeyValue value)[] sourceRows,
			(Row row, KeyValue value)[] targetRows,
			Association association)
		{
			var sourceValuesSet = sourceRows.Select(x => x.value).ToHashSet();

			var targetRowsMap =
				targetRows
					.Where(x => sourceValuesSet.Contains(x.value))
					.GroupBy(x => x.value)
					.ToDictionary(
						gr => gr.Key,
						gr => gr.Select(x => x.row));

			return sourceRows
				.SelectMany(row =>
				{
					var referencedRows = targetRowsMap.TryGetValue(row.value, out var rs)
						? rs
						: new Row[0];

					return referencedRows
						.Select(r => new Relation<Row>(row.row, r, association))
						.ToArray();
				})
				.ToArray();
		}

		private static Func<Table, Key, (Row, KeyValue)[]> BuildRowGetter(
			Table[] tables,
			Func<Table, Relation<Table>[]> getTableRelations)
		{
			Dictionary<(Table, Key), (Row, KeyValue)[]> valuesMap = tables.SelectMany(t =>
					getTableRelations(t).SelectMany(r => new[]
					{
						(target: t, key: r.Association.SourceKey),
						(target: r.Target, key: r.Association.TargetKey)
					}))
				.Distinct()
				.Select(r =>
				{
					var values = r.target.Rows
						.Select(tr => (tr.Value, tr.Value.GetValue(r.key)))
						.ToArray();

					return (reference: r, values);
				})
				.ToDictionary(
					x => (x.reference.target, x.reference.key),
					x => x.values);

			return (table, key) => valuesMap[(table, key)];
		}

		private static Func<T, Relation<T>[]> BuildRelationsGetter<T>(
			IReadOnlyCollection<MutualReference<T>> references) where T : class
		{
			var relationsMap =
				references
					.SelectMany(r => r.Relations)
					.GroupBy(r => r.Source)
					.ToDictionary(gr => gr.Key,
						gr => gr.Distinct().ToArray());

			return t => relationsMap[t];
		}
	}
}