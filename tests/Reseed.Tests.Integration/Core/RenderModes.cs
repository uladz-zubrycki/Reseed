using Reseed.Dsl;
using Reseed.Dsl.Cleanup;
using Reseed.Dsl.Simple;

namespace Reseed.Tests.Integration.Core
{
	public static class RenderModes
	{
		public static readonly RenderMode SimpleScriptPreferTruncate =
			RenderMode.Simple(
				SimpleInsertDefinition.Script(),
				CleanupDefinition.Script(
					CleanupOptions.IncludeNone(
						CleanupKind.PreferTruncate(),
						f => f.IncludeSchemas("dbo"))));
	}
}
