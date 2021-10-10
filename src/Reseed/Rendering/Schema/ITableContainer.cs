using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Ordering;
using Reseed.Schema;
using Reseed.Utils;
using static Reseed.Ordering.OrderedItem;

namespace Reseed.Rendering.Schema
{
	internal interface ITableContainer
	{
		IReadOnlyCollection<ObjectName> TableNames { get; }
		ITableContainer MapTableName([NotNull] Func<ObjectName, ObjectName> mapper);
	}

	internal sealed class Table : ITableContainer
	{
		public readonly TableDefinition Definition;
		public readonly IReadOnlyCollection<OrderedItem<Row>> Rows;

		public ObjectName Name => this.Definition.Name;
		public IReadOnlyCollection<Column> Columns => this.Definition.Columns;
		IReadOnlyCollection<ObjectName> ITableContainer.TableNames => new[] { Name };

		public Table(
			[NotNull] TableDefinition definition,
			[NotNull] IReadOnlyCollection<OrderedItem<Row>> rows)
		{
			if (rows == null) throw new ArgumentNullException(nameof(rows));
			if (rows.Count == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(rows));
			if (rows.Any(o => !o.Value.TableName.Equals(definition.Name)))
				throw new ArgumentException("Table can't contain other table's rows");

			this.Definition = definition ?? throw new ArgumentNullException(nameof(definition));
			this.Rows = rows;
		}

		public override string ToString() => this.Definition.ToString();

		public ITableContainer MapTableName(Func<ObjectName, ObjectName> mapper)
		{
			if (mapper == null) throw new ArgumentNullException(nameof(mapper));
			return new Table(
				this.Definition.MapTableName(mapper),
				this.Rows
					.Select(o => o.Map(r => r.MapTableName(mapper)))
					.ToArray());
		}
	}

	internal class MutualTableGroup : ITableContainer
	{
		private readonly Dictionary<ObjectName, TableDefinition> tableNameMap;
		public readonly IReadOnlyCollection<TableDefinition> Tables;
		public readonly IReadOnlyCollection<OrderedItem<Row>> Rows;

		public IReadOnlyCollection<ObjectName> TableNames { get; }

		public MutualTableGroup(
			[NotNull] IReadOnlyCollection<TableDefinition> tables,
			[NotNull] IReadOnlyCollection<OrderedItem<Row>> rows)
		{
			if (tables == null) throw new ArgumentNullException(nameof(tables));
			if (rows == null) throw new ArgumentNullException(nameof(rows));
			if (tables.Count == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(tables));
			if (rows.Count == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(rows));

			VerifyTablesUnique(tables);
			VerifyRowsOwnedByTables(tables, rows);

			this.Tables = tables;
			this.Rows = rows;
			this.tableNameMap = tables.ToDictionary(t => t.Name);
			TableNames = tables.Select(t => t.Name).ToArray();
		}

		public IReadOnlyCollection<OrderedItem<Table>> GetTables()
		{
			return Enumerate().ToArray();

			IEnumerable<OrderedItem<Table>> Enumerate()
			{
				TableDefinition currentTable = null;
				var tableRows = new List<OrderedItem<Row>>();
				var i = 0;

				foreach (var row in this.Rows.OrderBy(o => o.Order))
				{
					var rowTableName = row.Value.TableName;
					if (currentTable == null)
					{
						currentTable = this.tableNameMap[rowTableName];
					}

					if (!currentTable.Name.Equals(rowTableName))
					{
						yield return Ordered(i, new Table(currentTable, tableRows.ToArray()));
						currentTable = this.tableNameMap[rowTableName];
						tableRows = new List<OrderedItem<Row>> { row };
						i++;
					}
					else
					{
						tableRows.Add(row);
					}
				}

				if (currentTable != null)
				{
					yield return Ordered(i, new Table(currentTable, tableRows.ToArray()));
				}
			}
		}

		public override string ToString() =>
			$"{string.Join(", ", this.Tables.Select(t => t.ToString()))}";

		public virtual ITableContainer MapTableName(Func<ObjectName, ObjectName> mapper)
		{
			if (mapper == null) throw new ArgumentNullException(nameof(mapper));
			return new MutualTableGroup(
				this.Tables
					.Select(d => d.MapTableName(mapper))
					.ToArray(),
				this.Rows
					.Select(o => o.Map(r => r.MapTableName(mapper)))
					.ToArray());
		}
		
		private static void VerifyTablesUnique(IReadOnlyCollection<TableDefinition> tables)
		{
			var uniqueTables = tables.ToHashSet();
			if (uniqueTables.Count != tables.Count)
				throw new ArgumentException("Tables should be unique", nameof(tables));
		}

		private static void VerifyRowsOwnedByTables(
			IReadOnlyCollection<TableDefinition> tables,
			IReadOnlyCollection<OrderedItem<Row>> rows)
		{
			var tableNames = tables.Select(t => t.Name).ToHashSet();
			if (rows.Any(o => !tableNames.Contains(o.Value.TableName)))
				throw new ArgumentException("Mutually referent tables can't contain other table's rows");
		}
	}

	internal sealed class MutualRowGroup : MutualTableGroup
	{
		public readonly IReadOnlyCollection<Relation<ObjectName>> ForeignKeys;

		public MutualRowGroup(
			[NotNull] IReadOnlyCollection<TableDefinition> tables,
			[NotNull] IReadOnlyCollection<OrderedItem<Row>> rows,
			[NotNull] IReadOnlyCollection<Relation<ObjectName>> foreignKeys) 
			: base(tables, rows)
		{
			if (foreignKeys == null) throw new ArgumentNullException(nameof(foreignKeys));
			if (foreignKeys.Count == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(foreignKeys));

			var tableNames = tables.Select(t => t.Name).ToHashSet();
			if (foreignKeys.Any(a => !tableNames.Contains(a.Source) && !tableNames.Contains(a.Target)))
				throw new ArgumentException("Mutually referent rows can't contain other table's foreign keys");
			
			this.ForeignKeys = foreignKeys;
		}

		public override ITableContainer MapTableName(Func<ObjectName, ObjectName> mapper)
		{
			if (mapper == null) throw new ArgumentNullException(nameof(mapper));

			return new MutualRowGroup(
				this.Tables
					.Select(d => d.MapTableName(mapper))
					.ToArray(),
				this.Rows
					.Select(o => o.Map(r => r.MapTableName(mapper)))
					.ToArray(),
				this.ForeignKeys
					.Select(fk => fk.Map(mapper))
					.ToArray());
		}
	}
}