namespace Miki.API.Imageboards
{
    using Miki.API.Imageboards.Objects;
    using System;
    using System.Collections.Generic;

    public static class ImageboardProviderPool
    {
        private static readonly Dictionary<Type, ImageboardProvider> Providers =
            new Dictionary<Type, ImageboardProvider>();

        public static void AddProvider<T>(ImageboardProvider provider)
            where T : BooruPost
        {
            Providers.Add(typeof(T), provider);
        }

        public static ImageboardProvider GetProvider<T>()
            where T : BooruPost
        {
            if(Providers.ContainsKey(typeof(T)))
            {
                if(Providers.TryGetValue(typeof(T), out ImageboardProvider value))
                {
                    return value;
                }
            }

            return null;
        }
    }
}