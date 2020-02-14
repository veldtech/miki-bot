namespace Miki.Services.Blackjack
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Cache;
    using Exceptions;
    using Microsoft.EntityFrameworkCore;
    using Miki.Framework;
    using Patterns.Repositories;

    public class BlackjackRepository : IAsyncRepository<BlackjackContext>
    {
        private readonly IExtendedCacheClient cache;

        const string SessionKey = "blackjack:sessions";

        public BlackjackRepository(IExtendedCacheClient cache)
        {
            this.cache = cache;
        }

        /// <inheritdoc />
        public async ValueTask<BlackjackContext> GetAsync(params object[] id)
        {
            if (id.Length != 2)
            {
                throw new ArgumentOutOfRangeException();
            }
            var key = GetInstanceKey((ulong)id[0], (ulong)id[1]);
            var context = await cache.HashGetAsync<BlackjackContext>(SessionKey, key);
            Logging.Log.Debug($"GET - {SessionKey}: {key}");
            if (context != null)
            {

                context.ChannelId = (ulong) id[0];
                context.UserId = (ulong) id[1];
            }

            return context;
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<BlackjackContext>> ListAsync()
        {
            return new ValueTask<IEnumerable<BlackjackContext>>(
                cache.HashValuesAsync<BlackjackContext>(SessionKey));
        }

        /// <inheritdoc />
        public async ValueTask<BlackjackContext> AddAsync(BlackjackContext entity)
        {
            var value = await GetAsync(entity.ChannelId, entity.UserId);
            if (value != null)
            {
                throw new DuplicateSessionException();
            }
            var key = GetInstanceKey(entity.ChannelId, entity.UserId);

            Logging.Log.Debug($"ADD - {SessionKey}: {key}");
            await cache.HashUpsertAsync(SessionKey, key, entity);
            return entity;
        }

        /// <inheritdoc />
        public async ValueTask EditAsync(BlackjackContext entity)
        {
            var value = await GetAsync(entity.ChannelId, entity.UserId);
            if(value == null)
            {
                throw new BlackjackSessionNullException();
            }
            var key = GetInstanceKey(entity.ChannelId, entity.UserId);
            Logging.Log.Debug($"MUT - {SessionKey}: {key}");
            await cache.HashUpsertAsync(SessionKey, key, entity);
        }

        /// <inheritdoc />
        public async ValueTask DeleteAsync(BlackjackContext entity)
        {
            var key = GetInstanceKey(entity.ChannelId, entity.UserId);
            Logging.Log.Debug($"MUT - {SessionKey}: {key}");
            await cache.HashDeleteAsync(SessionKey, key);
        }

        private string GetInstanceKey(ulong channelId, ulong userId)
        {
            return $"{channelId}:{userId}";
        }
    }
}
