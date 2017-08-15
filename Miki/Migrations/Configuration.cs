namespace Miki.Migrations
{
    using EFCache;
    using EFCache.Redis;
    using System;
    using System.Collections.ObjectModel;
    using System.Configuration;
    using System.Data.Entity;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Migrations;

    internal sealed class MigrationConfiguration : DbMigrationsConfiguration<Miki.Models.MikiContext>
    {
        public MigrationConfiguration()
        {
            AutomaticMigrationsEnabled = false;
        }
    }

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