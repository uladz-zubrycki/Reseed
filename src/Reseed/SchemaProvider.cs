using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using JetBrains.Annotations;
using Reseed.Graphs;
using Reseed.Schema;
using Reseed.Schema.Internals;
using Testing.Common.Api.Schema;

namespace Reseed
{
	internal static class SchemaProvider
	{
		public static IReadOnlyCollection<TableSchema> LoadSchema([NotNull] string connectionString)
		{
			if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

			using var connection = new SqlConnection(connectionString);
			connection.Open();
			IReadOnlyCollection<TableData> tables = SchemaReader.LoadTables(connection);
			IReadOnlyCollection<Relation<TableData>> foreignKeys = SchemaReader.LoadForeignKeys(connection, tables);
			return NodeBuilder<TableSchema>.CollectNodes(
				tables, 
				foreignKeys, 
				(r, t) => r.Map(_ => t),
				CreateTableSchema);
		}

		private static TableSchema CreateTableSchema(
			TableData table,
			Reference<TableSchema>[] references) =>
			new TableSchema(
				table.Name,
				table.Columns,
				table.PrimaryKey,
				references);
	}
}