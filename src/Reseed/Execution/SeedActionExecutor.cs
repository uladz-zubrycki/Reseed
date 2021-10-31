using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using JetBrains.Annotations;
using Reseed.Generation;
using Reseed.Ordering;

namespace Reseed.Execution
{
	internal static class SeedActionExecutor
	{
		public static void Execute(
			[NotNull] string connectionString,
			[NotNull] IReadOnlyCollection<OrderedItem<ISeedAction>> actions)
		{
			if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
			if (actions == null) throw new ArgumentNullException(nameof(actions));
			if (actions.Count == 0)
			{
				return;
			}

			using var connection = new SqlConnection(connectionString);
			connection.Open();

			foreach (var action in actions.Order())
			{
				try
				{
					switch (action)
					{
						case SqlScriptAction script:
							ExecuteScript(connection, script);
							break;
						case SqlBulkCopyAction bulkCopy:
							ExecuteSqlBulkCopy(connection, bulkCopy);
							break;
						default:
							throw new NotSupportedException(
								$"Unknown {nameof(ISeedAction)} type {action.GetType().FullName}");
					}
				}
				catch (SqlException ex)
				{
					throw new SeedActionExecutionException(action, ex);
				}
			}
		}

		private static void ExecuteScript(SqlConnection connection, SqlScriptAction scriptAction)
		{
			using var command = connection.CreateCommand();
			command.CommandText = scriptAction.Text;
			command.ExecuteNonQuery();
		}

		private static void ExecuteSqlBulkCopy(
			SqlConnection connection,
			SqlBulkCopyAction action)
		{
			using var bulkCopy = new SqlBulkCopy(
				connection.ConnectionString,
				action.Options)
			{
				DestinationTableName = action.DestinationTable.GetSqlName()
			};

			foreach (var mapping in action.Columns)
			{
				bulkCopy.ColumnMappings.Add(mapping);
			}

			using DbCommand command = connection.CreateCommand();
			command.CommandText = action.SourceScript;
			using var reader = command.ExecuteReader();
			bulkCopy.WriteToServer(reader);
		}
	}
}
