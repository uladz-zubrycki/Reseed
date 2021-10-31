using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Ordering;
using Reseed.Schema;
using Reseed.Utils;

namespace Reseed.Rendering
{
	internal static class Scripts
	{
		public static SqlScriptAction RenderExecuteProcedureScript(string scriptName, ObjectName procedureName) => 
			new(scriptName, RenderExecuteProcedure(procedureName));

		public static SqlScriptAction RenderDropProcedureScript(string scriptName, ObjectName procedureName) => 
			new(scriptName, RenderDropProcedure(procedureName));

		public static string RenderCreateStoredProcedure([NotNull] ObjectName name, [NotNull] string body)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (body == null) throw new ArgumentNullException(nameof(body));

			return string.Join(Environment.NewLine + Environment.NewLine, $@"
				CREATE PROCEDURE {name.GetSqlName()}
				AS
				SET NOCOUNT ON;".TrimMargin(), 
				body);
		}

		public static string RenderDropProcedure([NotNull] ObjectName name)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			return $@"
				|IF (OBJECT_ID('{name.GetSqlName()}') IS NOT NULL)
				|	DROP PROCEDURE {name.GetSqlName()}"
				.TrimMargin('|');
		}

		public static string RenderExecuteProcedure([NotNull] ObjectName name)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			return $"EXEC {name.GetSqlName()}";
		}

		public static string RenderCreateSchema([NotNull] string schemaName)
		{
			if (schemaName == null) throw new ArgumentNullException(nameof(schemaName));
			return $@"
				|IF NOT EXISTS (SELECT * FROM sys.schemas s WHERE s.name = '{schemaName}')
				|	EXEC ('CREATE SCHEMA [{schemaName}]')"
				.TrimMargin('|');
		}

		public static string RenderDropSchema([NotNull] string schemaName)
		{
			if (schemaName == null) throw new ArgumentNullException(nameof(schemaName));
			return $@"
				|IF EXISTS (SELECT * FROM sys.schemas s WHERE s.name = '{schemaName}')
				|	EXEC ('DROP SCHEMA [{schemaName}]')"
				.TrimMargin('|');
		}

		public static string RenderDropTable([NotNull] ObjectName table)
		{
			if (table == null) throw new ArgumentNullException(nameof(table));
			return $@"
				|IF (OBJECT_ID('{table.GetSqlName()}') IS NOT NULL) 
				|	DROP TABLE {table.GetSqlName()}".TrimMargin('|');
		}

		public static string RenderDropForeignKeys(
			IEnumerable<Relation<TableSchema>> foreignKeys,
			bool checkTableExistence) =>
			string.Join(Environment.NewLine + Environment.NewLine,
				foreignKeys
					.GroupBy(fk => fk.Source)
					.Select(gr =>
					{
						var tableName = gr.Key.Name.GetSqlName();
						var constraintsScript = string.Join(Environment.NewLine,
								gr.Select(r =>
									$"CONSTRAINT IF EXISTS [{r.Association.Name}],"))
							.TrimEnd(',');

						var alterScript = $@"
							ALTER TABLE {tableName} 
							DROP
							{constraintsScript.WithMargin("\t", '|')}"
							.TrimMargin('|');

						var ifScript =
							checkTableExistence
								? $"IF (OBJECT_ID('{tableName}') IS NOT NULL)"
								: string.Empty;

						var bodyMargin = checkTableExistence ? "\t" : string.Empty;

						return $@"
							|{ifScript}
							{alterScript.WithMargin(bodyMargin, '|')}"
							.TrimMargin('|');
					}));

		public static string RenderCreateForeignKeys(
			IEnumerable<Relation<TableSchema>> foreignKeys) =>
			string.Join(Environment.NewLine + Environment.NewLine,
				foreignKeys
					.GroupBy(fk => fk.Source)
					.Select(gr =>
					{
						var constraintsScript = string.Join(
							"," + Environment.NewLine,
							gr.Select(r =>
									($@"|CONSTRAINT [{r.Association.Name}] 
									|FOREIGN KEY ({RenderKey(r.Association.SourceKey)}) " +
									 $"REFERENCES {r.Target.Name.GetSqlName()} ({RenderKey(r.Association.TargetKey)})")
									.TrimMargin('|'))
								.ToArray());

						return $@"
							|ALTER TABLE {gr.Key.Name.GetSqlName()} 
							|ADD {constraintsScript
								.WithMargin("\t", '|')
								.TrimStart('|')
								.TrimStart()}"
							.TrimMargin('|');
					}));

		public static string RenderKey([NotNull] Key key)
		{
			if (key == null) throw new ArgumentNullException(nameof(key));
			return string.Join(", ", 
				key.Columns
					.Order()
					.Select(k => $"[{k}]"));
		}
	}
}