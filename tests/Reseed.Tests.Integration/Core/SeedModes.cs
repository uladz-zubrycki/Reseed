using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework.Internal;
using Reseed.Configuration;
using Reseed.Configuration.Basic;
using Reseed.Configuration.Cleanup;
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

		private static BasicInsertDefinition BasicProcedureDefinition => 
			BasicInsertDefinition.Procedure(new ObjectName("spDeleteData"));

		private static TemporaryTablesInsertDefinition TempTablesProcedureDefinition => 
			TemporaryTablesInsertDefinition.Procedure(new ObjectName("spDeleteData"));

		public static readonly SeedMode BasicScriptDelete =
			SeedMode.Basic(
				BasicInsertDefinition.Script(),
				CleanupDefinition.Script(DeleteCleanupMode));

		public static readonly SeedMode BasicScriptPreferTruncate =
			SeedMode.Basic(
				BasicInsertDefinition.Script(),
				CleanupDefinition.Script(PreferTruncateCleanupMode));

		public static readonly SeedMode BasicScriptTruncate =
			SeedMode.Basic(
				BasicInsertDefinition.Script(),
				CleanupDefinition.Script(TruncateCleanupMode));

		public static readonly SeedMode BasicSpDelete =
			SeedMode.Basic(
				BasicProcedureDefinition,
				CleanupDefinition.Script(DeleteCleanupMode));

		public static readonly SeedMode BasicSpPreferTruncate =
			SeedMode.Basic(
				BasicProcedureDefinition,
				CleanupDefinition.Script(PreferTruncateCleanupMode));

		public static readonly SeedMode BasicSpTruncate =
			SeedMode.Basic(
				BasicProcedureDefinition,
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
