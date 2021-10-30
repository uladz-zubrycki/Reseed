using System;
using JetBrains.Annotations;
using Reseed.Utils;
using Testing.Common.Api.Schema;

namespace Reseed.Schema
{
	internal sealed class Relation<T> : IEquatable<Relation<T>>
	{
		public readonly T Source;
		public readonly T Target;
		public readonly Association Association;
		
		public string Name => this.Association.Name;

		public Relation(
			[NotNull] T source,
			[NotNull] T target,
			[NotNull] Association association)
		{
			this.Source = source ?? throw new ArgumentNullException(nameof(source));
			this.Target = target ?? throw new ArgumentNullException(nameof(target));
			this.Association = association ?? throw new ArgumentNullException(nameof(association));
		}

		public override string ToString() => 
			$"{Name}: {this.Source}({this.Association.SourceKey}) -> {this.Target}({this.Association.TargetKey})";

		public Relation<TOut> Map<TOut>([NotNull] Func<T, TOut> mapItem) =>
			Map(mapItem, Fn.Identity<Association>());

		public Relation<TOut> Map<TOut>(
			[NotNull] Func<T, TOut> mapItem,
			[NotNull] Func<Association, Association> mapAssociation)
		{
			if (mapItem == null) throw new ArgumentNullException(nameof(mapItem));
			if (mapAssociation == null) throw new ArgumentNullException(nameof(mapAssociation));

			return new Relation<TOut>(
				mapItem(this.Source), 
				mapItem(this.Target), 
				mapAssociation(this.Association));
		}

		public override bool Equals(object obj) => Equals(obj as Relation<T>);

		public bool Equals(Relation<T> other) =>
			other is not null &&
			(ReferenceEquals(other, this) || Equals(this.Association, other.Association));

		public override int GetHashCode() => 
			(this.Association != null ? this.Association.GetHashCode() : 0);
	}
}