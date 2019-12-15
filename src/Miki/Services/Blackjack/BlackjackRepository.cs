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
            var context = await new ValueTask<BlackjackContext>(
                cache.HashGetAsync<BlackjackContext>(
                    SessionKey, 
                    GetInstanceKey((ulong)id[0], (ulong)id[1])));
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
        public async ValueTask AddAsync(BlackjackContext entity)
        {
            var value = await GetAsync(entity.ChannelId, entity.UserId);
            if (value != null)
            {
                throw new DuplicateSessionException();
            }
            await cache.HashUpsertAsync(
                SessionKey, 
                GetInstanceKey(entity.ChannelId, entity.UserId), 
                entity);
        }

        /// <inheritdoc />
        public async ValueTask EditAsync(BlackjackContext entity)
        {
            var value = await GetAsync(entity.ChannelId, entity.UserId);
            if(value == null)
            {
                throw new BlackjackSessionNullException();
            }
            await cache.HashUpsertAsync(
                SessionKey,
                GetInstanceKey(entity.ChannelId, entity.UserId),
                entity);
        }

        /// <inheritdoc />
        public ValueTask DeleteAsync(BlackjackContext entity)
        {
            return new ValueTask(cache.HashDeleteAsync(
                SessionKey,
                GetInstanceKey(entity.ChannelId, entity.UserId)));
        }

        private string GetInstanceKey(ulong channelId, ulong userId)
        {
            return $"{channelId}:{userId}";
        }
    }
}
