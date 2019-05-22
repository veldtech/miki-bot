using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Miki.Localization
{
	// TODO Move this to Miki.Localization
	public class AggregateResourceManager : IResourceManager
	{
		private readonly IResourceManager[] _resourceManagers;

		public AggregateResourceManager(params IResourceManager[] resourceManagers)
			: this(resourceManagers.AsEnumerable())
		{
		}

		public AggregateResourceManager(IEnumerable<IResourceManager> resourceManager)
		{
			_resourceManagers = resourceManager as IResourceManager[] ?? resourceManager.ToArray();
		}

		public string GetString(string key)
		{
			return _resourceManagers
				.Select(resourceManager => resourceManager.GetString(key))
				.FirstOrDefault(str => !string.IsNullOrEmpty(str));
		}
	}
}
