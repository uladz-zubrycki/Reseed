using System.Collections.Generic;
using Testing.Common.Api.Schema;

namespace Reseed.Internals.Graphs
{
	internal interface IMutableNode<T> : INode<T> where T : IMutableNode<T>
	{
		void AddReferences(IReadOnlyCollection<Reference<T>> references);
	}
}