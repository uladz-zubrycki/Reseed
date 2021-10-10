using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Reseed.Schema;

namespace Testing.Common.Api.Schema
{
	internal sealed class Reference<T> : IEquatable<Reference<T>>
	{
		public readonly Association Association;
		public readonly T Target;

		public Reference(
			[NotNull] Association association,
			[NotNull] T target)
		{
			this.Association = association ?? throw new ArgumentNullException(nameof(association));
			this.Target = target ?? throw new ArgumentNullException(nameof(target));
		}

		public override string ToString() => 
			$"({this.Association.SourceKey}) -> {this.Target}({this.Association.TargetKey}) {this.Association.Name}";

		public Reference<TOut> Map<TOut>([NotNull] Func<T, TOut> mapTarget) =>
			Map(mapTarget, _ => _);

		public Reference<TOut> Map<TOut>(
			[NotNull] Func<T, TOut> mapTarget,
			[NotNull] Func<Association, Association> mapAssociation)
		{
			if (mapTarget == null) throw new ArgumentNullException(nameof(mapTarget));
			if (mapAssociation == null) throw new ArgumentNullException(nameof(mapAssociation));

			return new Reference<TOut>(
				mapAssociation(this.Association),
				mapTarget(this.Target));
		}

		public override bool Equals(object obj) => Equals(obj as Reference<T>);

		public bool Equals(Reference<T> other) =>
			other is not null &&
			(ReferenceEquals(other, this) ||
			 Equals(Association, other.Association) &&
			 Equals(Target, other.Target));

		public override int GetHashCode()
		{
			var hashCode = -409778428;
			hashCode = hashCode * -1521134295 + EqualityComparer<Association>.Default.GetHashCode(Association);
			hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(Target);
			return hashCode;
		}
	}
}