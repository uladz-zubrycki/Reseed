using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework.Internal;
using Reseed.Configuration;
using Reseed.Configuration.Basic;
using Reseed.Configuration.Cleanup;
using Reseed.Configuration.TemporaryTables;
using Reseed.Data;
using Reseed.Schema;

namespace Reseed.Tests.Integration.Core
{
	public static class SeedModes
	{
		private static readonly CleanupTarget CleanupTarget =
			CleanupTarget.Including(f => f.IncludeSchemas("dbo"));

		private static BasicInsertDefinition BasicProcedureDefinition =>
			BasicInsertDefinition.Procedure(new ObjectName("spDeleteData"));

		private static TemporaryTablesInsertDefinition TempTablesProcedureDefinition =>
			TemporaryTablesInsertDefinition.Procedure(new ObjectName("spDeleteData"));

		public static readonly Func<IDataProvider, SeedMode> BasicScriptDelete =
			dp => SeedMode.Basic(
				BasicInsertDefinition.Script(),
				CleanupDefinition.Script(CleanupMode.Delete(), CleanupTarget),
				dp);

		public static readonly Func<IDataProvider, SeedMode> BasicScriptPreferTruncate =
			dp => SeedMode.Basic(
				BasicInsertDefinition.Script(),
				CleanupDefinition.Script(CleanupMode.PreferTruncate(), CleanupTarget),
				dp);

		public static readonly Func<IDataProvider, SeedMode> BasicScriptTruncate =
			dp => SeedMode.Basic(
				BasicInsertDefinition.Script(),
				CleanupDefinition.Script(CleanupMode.Truncate(), CleanupTarget),
				dp);

		public static readonly Func<IDataProvider, SeedMode> BasicSpDelete =
			dp => SeedMode.Basic(
				BasicProcedureDefinition,
				CleanupDefinition.Script(CleanupMode.Delete(), CleanupTarget),
				dp);

		public static readonly Func<IDataProvider, SeedMode> BasicSpPreferTruncate =
			dp => SeedMode.Basic(
				BasicProcedureDefinition,
				CleanupDefinition.Script(CleanupMode.PreferTruncate(), CleanupTarget),
				dp);

		public static readonly Func<IDataProvider, SeedMode> BasicSpTruncate =
			dp => SeedMode.Basic(
				BasicProcedureDefinition,
				CleanupDefinition.Script(CleanupMode.Truncate(), CleanupTarget),
				dp);

		public static readonly Func<IDataProvider, SeedMode> TempTablesScriptDelete =
			dp => SeedMode.TemporaryTables(
				"temp",
				TemporaryTablesInsertDefinition.Script(),
				CleanupDefinition.Script(CleanupMode.Delete(), CleanupTarget),
				dp);

		public static readonly Func<IDataProvider, SeedMode> TempTablesScriptPreferTruncate =
			dp => SeedMode.TemporaryTables(
				"temp",
				TemporaryTablesInsertDefinition.Script(),
				CleanupDefinition.Script(CleanupMode.PreferTruncate(), CleanupTarget),
				dp);

		public static readonly Func<IDataProvider, SeedMode> TempTablesScriptTruncate =
			dp => SeedMode.TemporaryTables(
				"temp",
				TemporaryTablesInsertDefinition.Script(),
				CleanupDefinition.Script(CleanupMode.Truncate(), CleanupTarget),
				dp);

		public static readonly Func<IDataProvider, SeedMode> TempTablesSpDelete =
			dp => SeedMode.TemporaryTables(
				"temp",
				TempTablesProcedureDefinition,
				CleanupDefinition.Script(CleanupMode.Delete(), CleanupTarget),
				dp);

		public static readonly Func<IDataProvider, SeedMode> TempTablesSpPreferTruncate =
			dp => SeedMode.TemporaryTables(
				"temp",
				TempTablesProcedureDefinition,
				CleanupDefinition.Script(CleanupMode.PreferTruncate(), CleanupTarget),
				dp);

		public static readonly Func<IDataProvider, SeedMode> TempTablesSpTruncate =
			dp => SeedMode.TemporaryTables(
				"temp",
				TempTablesProcedureDefinition,
				CleanupDefinition.Script(CleanupMode.Delete(), CleanupTarget),
				dp);

		public static TestFixtureParameters[] Every()
		{
			var seedModeType = typeof(Func<IDataProvider, SeedMode>);
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