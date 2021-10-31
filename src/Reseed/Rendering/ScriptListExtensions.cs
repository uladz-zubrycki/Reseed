using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Reseed.Rendering
{
	public static class ScriptListExtensions
	{
		public static List<SqlScriptAction> AddScript(
			[NotNull] this List<SqlScriptAction> scripts,
			[NotNull] SqlScriptAction scriptAction)
		{
			if (scripts == null) throw new ArgumentNullException(nameof(scripts));
			if (scriptAction == null) throw new ArgumentNullException(nameof(scriptAction));
			scripts.Add(scriptAction);
			return scripts;
		}

		public static List<SqlScriptAction> AddScriptWhen(
			[NotNull] this List<SqlScriptAction> scripts,
			[NotNull] Func<SqlScriptAction> factory,
			bool condition)
		{
			if (scripts == null) throw new ArgumentNullException(nameof(scripts));
			if (factory == null) throw new ArgumentNullException(nameof(factory));
			if (condition)
			{
				scripts.Add(factory());
			}

			return scripts;
		}
	}
}