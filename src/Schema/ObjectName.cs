using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Reseed.Schema
{
	public sealed class ObjectName : IEquatable<ObjectName>
	{
		public readonly string Schema;
		public readonly string Name;

		public ObjectName([NotNull] string name, [NotNull] string schema = "dbo")
		{
			this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		public string GetSqlName(bool escape = true) =>
			escape
				? $"[{this.Schema}].[{this.Name}]"
				: $"{this.Schema}.{this.Name}";

		public override bool Equals(object obj) => Equals(obj as ObjectName);

		public bool Equals(ObjectName other) =>
			other != null
			&& this.Schema.Equals(other.Schema, StringComparison.OrdinalIgnoreCase)
			&& this.Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);

		public override int GetHashCode()
		{
			var hashCode = 1264356421;
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Schema.ToLowerInvariant());
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Name.ToLowerInvariant());
			return hashCode;
		}

		public override string ToString() => $"{this.Schema}.{this.Name}";
	}
}