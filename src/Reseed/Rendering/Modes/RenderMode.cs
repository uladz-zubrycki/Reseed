using System;
using JetBrains.Annotations;

namespace Reseed.Rendering.Modes
{
	[PublicAPI]
	public abstract class RenderMode
	{
		public static RenderMode Simple(
			[NotNull] SimpleInsertMode insertMode,
			[NotNull] CleanupMode cleanupMode) =>
			new SimpleMode(insertMode, cleanupMode);

		public static RenderMode TemporaryTables(
			[NotNull] string schemaName,
			[NotNull] TemporaryTablesInsertMode insertMode,
			[NotNull] CleanupMode cleanupMode) =>
			new TemporaryTablesMode(schemaName, insertMode, cleanupMode);
	}

	internal sealed class SimpleMode : RenderMode
	{
		public readonly SimpleInsertMode InsertMode;
		public readonly CleanupMode CleanupMode;

		public SimpleMode(
			[NotNull] SimpleInsertMode insertMode,
			[NotNull] CleanupMode cleanupMode)
		{
			this.InsertMode = insertMode ?? throw new ArgumentNullException(nameof(insertMode));
			this.CleanupMode = cleanupMode ?? throw new ArgumentNullException(nameof(cleanupMode));
		}
	}

	internal sealed class TemporaryTablesMode : RenderMode
	{
		public readonly string SchemaName;
		public readonly TemporaryTablesInsertMode InsertMode;
		public readonly CleanupMode CleanupMode;

		public TemporaryTablesMode(
			[NotNull] string schemaName,
			[NotNull] TemporaryTablesInsertMode insertMode,
			[NotNull] CleanupMode cleanupMode)
		{
			if (string.IsNullOrEmpty(schemaName))
				throw new ArgumentException("Value cannot be null or empty.", nameof(schemaName));
			this.SchemaName = schemaName;
			
			this.InsertMode = insertMode ?? throw new ArgumentNullException(nameof(insertMode));
			this.CleanupMode = cleanupMode ?? throw new ArgumentNullException(nameof(cleanupMode));
		}
	}
}