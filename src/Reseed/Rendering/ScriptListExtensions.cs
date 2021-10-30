using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Reseed.Rendering
{
	public static class ScriptListExtensions
	{
		public static List<DbScript> AddScript(
			[NotNull] this List<DbScript> scripts,
			[NotNull] DbScript script)
		{
			if (scripts == null) throw new ArgumentNullException(nameof(scripts));
			if (script == null) throw new ArgumentNullException(nameof(script));
			scripts.Add(script);
			return scripts;
		}

		public static List<DbScript> AddScriptWhen(
			[NotNull] this List<DbScript> scripts,
			[NotNull] Func<DbScript> factory,
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