using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Reseed.Schema.Internals;
using Reseed.Utils;
using Testing.Common.Api.Schema;

namespace Reseed.Graphs
{
	internal sealed class ReferencePath<T> where T: class
	{
		private readonly HashSet<T> items;
		public readonly T Source;
		public readonly IReadOnlyCollection<Reference<T>> References;
		public readonly IReadOnlyCollection<Relation<T>> Relations;

		public T Target => this.References.LastOrDefault()?.Target ?? this.Source;
		public IReadOnlyCollection<T> Items => this.items;

		public ReferencePath([NotNull] T source)
			: this(source, Array.Empty<Reference<T>>()) { }

		public ReferencePath([NotNull] T source, [NotNull] IReadOnlyCollection<Reference<T>> references)
		{
			this.Source = source ?? throw new ArgumentNullException(nameof(source));
			this.References = references ?? throw new ArgumentNullException(nameof(references));
			this.items = CollectItems(source, references);
			this.Relations = CollectRelations(source, references);
		}

		public bool Contains(T item) => this.items.Contains(item);

		public ReferencePath<T> Append(Reference<T> reference) => 
			new ReferencePath<T>(this.Source, this.References.Append(reference).ToArray());

		public ReferencePath<T> StartFrom([NotNull] T item)
		{
			if (item == null) throw new ArgumentNullException(nameof(item));
			if (!Contains(item))
			{
				throw new InvalidOperationException($"Item '{item}' is not found in reference path '{this}'");
			}

			if (this.Source == item)
			{
				return this;
			}
			else if (this.References.Count == 0)
			{
				return new ReferencePath<T>(item);
			}
			else
			{
				int startIndex = this.References
					.Select((r, i) => (r.Target, index: i))
					.First(pair => pair.Target == item)
					.index;

				return new ReferencePath<T>(item, this.References.Skip(startIndex + 1).ToArray());
			}
		}

		public override string ToString()
		{
			string path = string.Join(" -> ", this.References.Select(t => t.Target.ToString()));
			return $"{this.Source}" +
			       (string.IsNullOrEmpty(path) ? "" : $" -> {path}");
		}

		public ReferencePath<TOut> Map<TOut>([NotNull] Func<T, TOut> mapper) where TOut : class
		{
			if (mapper == null) throw new ArgumentNullException(nameof(mapper));
			return new ReferencePath<TOut>(
				mapper(this.Source),
				this.References
					.Select(r => r.Map(mapper))
					.ToArray());
		}

		private static HashSet<T> CollectItems(T source, IReadOnlyCollection<Reference<T>> references) =>
			references.Select(r => r.Target)
				.Append(source)
				.ToHashSet();

		private static Relation<T>[] CollectRelations(T source, IReadOnlyCollection<Reference<T>> references) =>
			references
				.Select(r => r.Target)
				.Prepend(source)
				.Zip(references, (s, r) => 
					new Relation<T>(s, r.Target, r.Association))
				.ToArray();
	}
}