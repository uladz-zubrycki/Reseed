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
			[NotNull] SqlConnection connection,
			[NotNull] IReadOnlyCollection<OrderedItem<ISeedAction>> actions,
			TimeSpan? actionTimeout)
		{
			if (connection == null) throw new ArgumentNullException(nameof(connection));
			if (actions == null) throw new ArgumentNullException(nameof(actions));

			foreach (var action in actions.Order())
			{
				try
				{
					switch (action)
					{
						case SqlScriptAction script:
							ExecuteScript(connection, script, actionTimeout);
							break;
						case SqlBulkCopyAction bulkCopy:
							ExecuteSqlBulkCopy(connection, bulkCopy, actionTimeout);
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

		private static void ExecuteScript(
			SqlConnection connection,
			SqlScriptAction scriptAction,
			TimeSpan? timeout)
		{
			using var command = CreateCommand(connection, scriptAction.Text, timeout);
			command.ExecuteNonQuery();
		}

		private static void ExecuteSqlBulkCopy(
			SqlConnection connection,
			SqlBulkCopyAction action,
			TimeSpan? timeout)
		{
			using var bulkCopy = new SqlBulkCopy(
				connection.ConnectionString,
				action.Options)
			{
				DestinationTableName = action.DestinationTable.GetSqlName(),
			};

			if (timeout != null)
			{
				bulkCopy.BulkCopyTimeout = (int)timeout.Value.TotalSeconds;
			}

			foreach (var mapping in action.Columns)
			{
				bulkCopy.ColumnMappings.Add(mapping);
			}

			using var sourceCommand = CreateCommand(connection, action.SourceScript, timeout);
			using var sourceReader = sourceCommand.ExecuteReader();
			bulkCopy.WriteToServer(sourceReader);
		}

		private static DbCommand CreateCommand(
			SqlConnection connection, 
			string commandText, 
			TimeSpan? timeout)
		{
			DbCommand command = connection.CreateCommand();
			command.CommandText = commandText;
			if (timeout != null)
			{
				command.CommandTimeout = (int)timeout.Value.TotalSeconds;
			}

			return command;
		}
	}
}