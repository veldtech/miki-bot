using Miki.Cache;
using Miki.Services.Blackjack.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Services
{
    public class BlackjackService
    {
        private readonly IExtendedCacheClient _cache;

        private string GetSessionKey(ulong channelId)
            => "sessions:blackjack:" + channelId;

        public BlackjackService(IExtendedCacheClient cache)
        {
            _cache = cache;
        }

        public async Task CreateNewAsync(ulong userId, ulong channelId)
        {
            if (await _cache.HashExistsAsync(GetSessionKey(channelId), userId.ToString()))
            {
                throw new BlackjackSessionExistsException();
            }

            // TODO (Veld): Finish this service.
        }
    }
}
