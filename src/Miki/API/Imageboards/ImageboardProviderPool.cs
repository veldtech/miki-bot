using Miki.API.Imageboards.Objects;
using System;
using System.Collections.Generic;

namespace Miki.API.Imageboards
{
	public static class ImageboardProviderPool
	{
		private static readonly Dictionary<Type, ImageboardProvider<BooruPost>> Providers = new Dictionary<Type, ImageboardProvider<BooruPost>>();

		public static void AddProvider<T>(ImageboardProvider<T> provider) where T : BooruPost
		{
			ImageboardProvider<BooruPost> newProvider = new ImageboardProvider<BooruPost>(provider.Config);

			Providers.Add(typeof(T), newProvider);
		}

		public static ImageboardProvider<T> GetProvider<T>() where T : BooruPost
		{
			if (Providers.ContainsKey(typeof(T)))
			{
				if (Providers.TryGetValue(typeof(T), out ImageboardProvider<BooruPost> value))
				{
					ImageboardProvider<T> output = new ImageboardProvider<T>(value.Config);
					return output;
				}
			}
			return null;
		}
	}
}