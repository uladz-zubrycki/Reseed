using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework.Internal;
using Reseed.Configuration;
using Reseed.Configuration.Cleanup;
using Reseed.Configuration.Simple;
using Reseed.Configuration.TemporaryTables;
using Reseed.Schema;

namespace Reseed.Tests.Integration.Core
{
	public static class SeedModes
	{
		private static readonly Func<IncludingCleanupFilter, IncludingCleanupFilter> 
			ConfigureCleanup = f => f.IncludeSchemas("dbo");

		private static readonly CleanupConfiguration DeleteCleanupMode = 
			CleanupConfiguration.IncludeNone(CleanupMode.Delete(), ConfigureCleanup);

		private static readonly CleanupConfiguration PreferTruncateCleanupMode = 
			CleanupConfiguration.IncludeNone(CleanupMode.PreferTruncate(), ConfigureCleanup);

		private static readonly CleanupConfiguration TruncateCleanupMode = 
			CleanupConfiguration.IncludeNone(CleanupMode.Truncate(), ConfigureCleanup);

		private static SimpleInsertDefinition SimpleProcedureDefinition => 
			SimpleInsertDefinition.Procedure(new ObjectName("spDeleteData"));

		private static TemporaryTablesInsertDefinition TempTablesProcedureDefinition => 
			TemporaryTablesInsertDefinition.Procedure(new ObjectName("spDeleteData"));

		public static readonly SeedMode SimpleScriptDelete =
			SeedMode.Simple(
				SimpleInsertDefinition.Script(),
				CleanupDefinition.Script(DeleteCleanupMode));

		public static readonly SeedMode SimpleScriptPreferTruncate =
			SeedMode.Simple(
				SimpleInsertDefinition.Script(),
				CleanupDefinition.Script(PreferTruncateCleanupMode));

		public static readonly SeedMode SimpleScriptTruncate =
			SeedMode.Simple(
				SimpleInsertDefinition.Script(),
				CleanupDefinition.Script(TruncateCleanupMode));

		public static readonly SeedMode SimpleSpDelete =
			SeedMode.Simple(
				SimpleProcedureDefinition,
				CleanupDefinition.Script(DeleteCleanupMode));

		public static readonly SeedMode SimpleSpPreferTruncate =
			SeedMode.Simple(
				SimpleProcedureDefinition,
				CleanupDefinition.Script(PreferTruncateCleanupMode));

		public static readonly SeedMode SimpleSpTruncate =
			SeedMode.Simple(
				SimpleProcedureDefinition,
				CleanupDefinition.Script(TruncateCleanupMode));

		public static readonly SeedMode TempTablesScriptDelete =
			SeedMode.TemporaryTables(
				"temp",
				TemporaryTablesInsertDefinition.Script(), 
				CleanupDefinition.Script(DeleteCleanupMode));

		public static readonly SeedMode TempTablesScriptPreferTruncate =
			SeedMode.TemporaryTables(
				"temp",
				TemporaryTablesInsertDefinition.Script(), 
				CleanupDefinition.Script(PreferTruncateCleanupMode));

		public static readonly SeedMode TempTablesScriptTruncate =
			SeedMode.TemporaryTables(
				"temp",
				TemporaryTablesInsertDefinition.Script(), 
				CleanupDefinition.Script(TruncateCleanupMode));

		public static readonly SeedMode TempTablesSpDelete =
			SeedMode.TemporaryTables(
				"temp",
				TempTablesProcedureDefinition,
				CleanupDefinition.Script(DeleteCleanupMode));

		public static readonly SeedMode TempTablesSpPreferTruncate =
			SeedMode.TemporaryTables(
				"temp",
				TempTablesProcedureDefinition,
				CleanupDefinition.Script(PreferTruncateCleanupMode));

		public static readonly SeedMode TempTablesSpTruncate =
			SeedMode.TemporaryTables(
				"temp",
				TempTablesProcedureDefinition,
				CleanupDefinition.Script(TruncateCleanupMode));

		public static TestFixtureParameters[] Every()
		{
			var seedModeType = typeof(SeedMode);
			return typeof(SeedModes)
				.GetFields(BindingFlags.Public | BindingFlags.Static)
				.Where(f => f.FieldType == seedModeType)
				.Select(f => (name: f.Name, value: f.GetValue(null)))
				.Select(f => new TestFixtureParameters(f.value)
				{
					TestName = f.name
				})
				.ToArray();
		}
	}
}
