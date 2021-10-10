using System;
using JetBrains.Annotations;
using Reseed.Schema;

namespace Reseed.Rendering.Decorators
{
	internal sealed class IdentityInsertDecorator : IScriptDecorator
	{
		private readonly ObjectName table;
		private readonly bool hasIdentity;

		public IdentityInsertDecorator([NotNull] ObjectName table, bool hasIdentity)
		{
			this.table = table ?? throw new ArgumentNullException(nameof(table));
			this.hasIdentity = hasIdentity;
		}

		public string Decorate([NotNull] string script)
		{
			if (script == null) throw new ArgumentNullException(nameof(script));
			return !this.hasIdentity
				? script
				: string.Join(Environment.NewLine,
					Render("ON"),
					script,
					Render("OFF"));

			string Render(string keyword) =>
				$"SET IDENTITY_INSERT {this.table.GetSqlName()} {keyword}";
		}
	}
}