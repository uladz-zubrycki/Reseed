using System;
using Reseed.Data.Providers.FileSystem;

namespace Reseed.Data
{
	internal abstract class EntityOrigin: IEquatable<EntityOrigin>
	{
		public abstract string OriginName { get; }

		public override bool Equals(object obj) => Equals(obj as DataFile);

		public bool Equals(EntityOrigin other) =>
			other is not null &&
			(ReferenceEquals(other, this) || Equals(this.OriginName, other.OriginName));

		public override int GetHashCode() => this.OriginName.GetHashCode();

		public override string ToString() => this.OriginName;
	}
}
