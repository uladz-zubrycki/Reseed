using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Schema;

namespace Reseed.Rendering.Insertion
{
	internal sealed class DisableForeignKeysDecorator : IScriptDecorator
	{
		private readonly IReadOnlyCollection<Relation<ObjectName>> foreignKeys;

		public DisableForeignKeysDecorator(
			[NotNull] IReadOnlyCollection<Relation<ObjectName>> foreignKeys)
		{
			this.foreignKeys = foreignKeys ?? throw new ArgumentNullException(nameof(foreignKeys));
		}

		public string Decorate([NotNull] string script)
		{
			if (script == null) throw new ArgumentNullException(nameof(script));
			return this.foreignKeys.Count == 0
				? script
				: string.Join(Environment.NewLine,
					Render("NOCHECK"),
					script,
					Render("CHECK"));

			string Render(string keyword) =>
				string.Join(Environment.NewLine, this.foreignKeys
					.GroupBy(fk => fk.Source)
					.SelectMany(gr => gr.Select(a =>
						$"ALTER TABLE {gr.Key.GetSqlName()} " +
						$"{keyword} CONSTRAINT [{a.Association.Name}]")));
		}
	}
}