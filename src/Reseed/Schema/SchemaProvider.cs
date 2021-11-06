using System;
using System.Collections.Generic;
using Reseed.Schema.Providers;

namespace Reseed.Schema
{
	internal static class SchemaProvider
	{
		public static IReadOnlyCollection<TableSchema> Load(string connectionString)
		{
			var schemas = MsSqlSchemaProvider.LoadSchema(connectionString);
			if (schemas.Count == 0)
			{
				throw BuildNoSchemasException();
			}

			return schemas;
		}

		private static InvalidOperationException BuildNoSchemasException() =>
			new("The specified database doesn't contain any tables, " +
			    $"therefore can't be used as {nameof(Reseeder)} target. " +
			    "Make sure that all the database migrations are properly applied if any" +
			    $"before using {nameof(Reseeder)}");
	}
}
