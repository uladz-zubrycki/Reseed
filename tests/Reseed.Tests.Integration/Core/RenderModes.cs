using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Reseed.Dsl;
using Reseed.Dsl.Cleanup;
using Reseed.Dsl.Simple;
using Reseed.Dsl.TemporaryTables;
using Reseed.Schema;

namespace Reseed.Tests.Integration.Core
{
	public static class RenderModes
	{
		private static readonly Func<IncludingDataCleanupFilter, IncludingDataCleanupFilter> 
			ConfigureCleanup = f => f.IncludeSchemas("dbo");

		private static readonly CleanupOptions DeleteCleanupMode = 
			CleanupOptions.IncludeNone(CleanupKind.Delete(), ConfigureCleanup);

		private static readonly CleanupOptions PreferTruncateCleanupMode = 
			CleanupOptions.IncludeNone(CleanupKind.PreferTruncate(), ConfigureCleanup);

		private static readonly CleanupOptions TruncateCleanupMode = 
			CleanupOptions.IncludeNone(CleanupKind.Truncate(), ConfigureCleanup);

		private static SimpleInsertDefinition SimpleProcedureDefinition => 
			SimpleInsertDefinition.Procedure(new ObjectName("spDeleteData"));

		private static TemporaryTablesInsertDefinition TempTablesProcedureDefinition => 
			TemporaryTablesInsertDefinition.Procedure(new ObjectName("spDeleteData"));

		public static readonly RenderMode SimpleScriptDelete =
			RenderMode.Simple(
				SimpleInsertDefinition.Script(),
				CleanupDefinition.Script(DeleteCleanupMode));

		public static readonly RenderMode SimpleScriptPreferTruncate =
			RenderMode.Simple(
				SimpleInsertDefinition.Script(),
				CleanupDefinition.Script(PreferTruncateCleanupMode));

		public static readonly RenderMode SimpleScriptTruncate =
			RenderMode.Simple(
				SimpleInsertDefinition.Script(),
				CleanupDefinition.Script(TruncateCleanupMode));

		public static readonly RenderMode SimpleSpDelete =
			RenderMode.Simple(
				SimpleProcedureDefinition,
				CleanupDefinition.Script(DeleteCleanupMode));

		public static readonly RenderMode SimpleSpPreferTruncate =
			RenderMode.Simple(
				SimpleProcedureDefinition,
				CleanupDefinition.Script(PreferTruncateCleanupMode));

		public static readonly RenderMode SimpleSpTruncate =
			RenderMode.Simple(
				SimpleProcedureDefinition,
				CleanupDefinition.Script(TruncateCleanupMode));

		public static readonly RenderMode TempTablesScriptDelete =
			RenderMode.TemporaryTables(
				"temp",
				TemporaryTablesInsertDefinition.Script(), 
				CleanupDefinition.Script(DeleteCleanupMode));

		public static readonly RenderMode TempTablesScriptPreferTruncate =
			RenderMode.TemporaryTables(
				"temp",
				TemporaryTablesInsertDefinition.Script(), 
				CleanupDefinition.Script(PreferTruncateCleanupMode));

		public static readonly RenderMode TempTablesScriptTruncate =
			RenderMode.TemporaryTables(
				"temp",
				TemporaryTablesInsertDefinition.Script(), 
				CleanupDefinition.Script(TruncateCleanupMode));

		public static readonly RenderMode TempTablesSpDelete =
			RenderMode.TemporaryTables(
				"temp",
				TempTablesProcedureDefinition,
				CleanupDefinition.Script(DeleteCleanupMode));

		public static readonly RenderMode TempTablesSpPreferTruncate =
			RenderMode.TemporaryTables(
				"temp",
				TempTablesProcedureDefinition,
				CleanupDefinition.Script(PreferTruncateCleanupMode));

		public static readonly RenderMode TempTablesSpTruncate =
			RenderMode.TemporaryTables(
				"temp",
				TempTablesProcedureDefinition,
				CleanupDefinition.Script(TruncateCleanupMode));

		public static TestFixtureParameters[] Every()
		{
			var renderModeType = typeof(RenderMode);
			return typeof(RenderModes)
				.GetFields(BindingFlags.Public | BindingFlags.Static)
				.Where(f => f.FieldType == renderModeType)
				.Select(f => (name: f.Name, value: f.GetValue(null)))
				.Select(f => new TestFixtureParameters(f.value)
				{
					TestName = f.name
				})
				.ToArray();
		}
	}
}
