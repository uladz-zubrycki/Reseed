using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using JetBrains.Annotations;
using Reseed.Graphs;
using Reseed.Schema.Internals;
using Testing.Common.Api.Schema;

namespace Reseed.Schema
{
	internal static class MsSqlSchemaProvider
	{
		public static IReadOnlyCollection<TableSchema> LoadSchema([NotNull] string connectionString)
		{
			if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

			using var connection = new SqlConnection(connectionString);
			connection.Open();
			var tables = SchemaReader.LoadTables(connection);
			var foreignKeys = SchemaReader.LoadForeignKeys(connection, tables);
			return NodeBuilder<TableSchema>.CollectNodes(
				tables, 
				foreignKeys, 
				(r, t) => r.Map(_ => t),
				CreateTableSchema);
		}

		private static TableSchema CreateTableSchema(
			TableData table,
			Reference<TableSchema>[] references) =>
			new(
				table.Name,
				table.Columns,
				table.PrimaryKey,
				references);
	}
}