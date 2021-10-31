using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace Reseed.Utils
{
	internal static class SqlFormatter
	{
		/// <summary>
		/// Trims line margin, which is all whitespace characters to the left of the target one,
		/// and multiple empty lines from multiline strings.
		/// Might be useful for sql commands written as verbatim strings
		/// </summary>
		public static string TrimMargin([NotNull] this string value, char? separator = null)
		{
			if (value == null) throw new ArgumentNullException(nameof(value));
			return value.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
				.Select(line =>
				{
					var trimmed = line.TrimStart();
					return trimmed.StartsWith((separator ?? ' ').ToString()) 
						? trimmed.Substring(1) 
						: trimmed;
				}) 
				.MergeEmptyLines()
				.SkipWhile(string.IsNullOrWhiteSpace)
				.JoinStrings(Environment.NewLine);
		}

		public static string WithMargin([NotNull] this string value, string margin, char? separator = null)
		{
			if (value == null) throw new ArgumentNullException(nameof(value));
			var lines = value.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			return lines
				.Select(line => separator + margin + line) 
				.JoinStrings(Environment.NewLine);
		}

		public static string Wrap(
			[NotNull] this string value, 
			int maxLength,
			[NotNull] Func<string, string> mapWrapped,
			[NotNull] Func<string, bool> couldBreak,
			[NotNull] params char[] breakAt)
		{
			if (value == null) throw new ArgumentNullException(nameof(value));
			if (maxLength <= 0) throw new ArgumentOutOfRangeException(nameof(maxLength));
			if (mapWrapped == null) throw new ArgumentNullException(nameof(mapWrapped));
			if (couldBreak == null) throw new ArgumentNullException(nameof(couldBreak));
			if (breakAt == null) throw new ArgumentNullException(nameof(breakAt));
			if (breakAt.Length == 0)
				throw new ArgumentException("Value cannot be an empty collection.", nameof(breakAt));

			var breakAtSet = breakAt.ToHashSet();
			var lines = new List<string>();
			var buffer = new StringBuilder();

			foreach (var line in value.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
			{
				var i = 0;
				var wrapped = false;
				while (i < line.Length)
				{
					if (buffer.Length < maxLength)
					{
						buffer.Append(line[i]);
						i++;
					}
					else
					{
						while ((!breakAtSet.Contains(line[i - 1]) ||
						        !couldBreak(buffer.ToString())) &&
						       i < line.Length)
						{
							buffer.Append(line[i]);
							i++;
						}

						Yield(lines, buffer, wrapped, mapWrapped);
						wrapped = wrapped || breakAtSet.Contains(line[i - 1]);
					}
				}

				Yield(lines, buffer, wrapped, mapWrapped);
			}

			return string.Join(Environment.NewLine, lines);

			static void Yield(
				ICollection<string> result,
				StringBuilder buffer,
				bool map,
				Func<string, string> mapper)
			{
				if (buffer.Length > 0)
				{
					var value = buffer.ToString();
					result.Add(map ? mapper(value.TrimStart()) : value);
					buffer.Clear();
				}
			}
		}

		public static string MergeLines(
			[NotNull] this string value,
			int maxLength,
			string separator)
		{
			if (value == null) throw new ArgumentNullException(nameof(value));
			if (maxLength <= 0) throw new ArgumentOutOfRangeException(nameof(maxLength));

			var lines = value.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			var output = new List<string>();
			var buffer = new StringBuilder();

			foreach (var line in lines)
			{
				if (buffer.Length + line.Length < maxLength)
				{
					if (buffer.Length > 0)
					{
						buffer.Append(separator);
					}

					buffer.Append(line);
				}
				else
				{
					output.Add(buffer.ToString());
					buffer.Clear();
					buffer.Append(line);
				}
			}

			if (buffer.Length > 0)
			{
				output.Add(buffer.ToString());
			}

			return string.Join(Environment.NewLine, output);
		}

		public static string MergeEmptyLines([NotNull] this string value)
		{
			if (value == null) throw new ArgumentNullException(nameof(value));
			return value.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
				.MergeEmptyLines()
				.JoinStrings(Environment.NewLine);
		}

		private static IEnumerable<string> MergeEmptyLines([NotNull] this IEnumerable<string> lines)
		{
			var haveEmpty = false;
			foreach (var line in lines)
			{
				if (string.IsNullOrWhiteSpace(line))
				{
					if (!haveEmpty)
					{
						yield return line;
						haveEmpty = true;
					}
				}
				else
				{
					yield return line;
					haveEmpty = false;
				}
			}
		}
	}
}
