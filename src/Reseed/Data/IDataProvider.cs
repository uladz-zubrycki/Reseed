using System.Collections.Generic;

namespace Reseed.Data
{
	public interface IDataProvider
	{
		IReadOnlyCollection<Entity> GetEntities();
	}
}
