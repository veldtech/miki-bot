using EFCache;
using EFCache.Redis;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Models.Objects
{
    public class Configuration : DbConfiguration
    {
        public Configuration()
        {
            var redisConnection = ConfigurationManager.ConnectionStrings["Redis"].ToString();
            var cache = new RedisCache(redisConnection);
            var transactionHandler = new CacheTransactionHandler(cache);
            AddInterceptor(transactionHandler);

            Loaded += (sender, args) =>
            {
                args.ReplaceService<CachingProviderServices>(
                    (s, _) => new CachingProviderServices(s, transactionHandler, new RedisCachingPolicy())
                    );
            };
        }
    }

    public class RedisCachingPolicy : CachingPolicy
    {
        protected override void GetExpirationTimeout(ReadOnlyCollection<EntitySetBase> affectedEntitySets, out TimeSpan slidingExpiration, out DateTimeOffset absoluteExpiration)
        {
            slidingExpiration = TimeSpan.FromMinutes(5);
            absoluteExpiration = DateTimeOffset.Now.AddMinutes(30);
        }
    }
}
