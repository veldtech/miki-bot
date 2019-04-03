using Miki.Cache;
using Miki.Modules.Gambling.Services.Roulette.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules.Gambling.Services.Roulette
{
    public class RouletteService
    {
        private IExtendedCacheClient _cache;

        private static string CacheCollection = "roulette:rooms";

        public RouletteService(IExtendedCacheClient cache)
        {
            if(cache == null)
            {
                throw new NullReferenceException("cache cannot be null");
            }
            _cache = cache;
        }

        public async Task<RouletteTable> CreateTableAsync(ulong channelId, ulong userId)
        {
            RouletteTable table = new RouletteTable(userId);
            table.Bets = new List<RouletteBet>();
            await _cache.HashUpsertAsync(CacheCollection, channelId.ToString(), table);
            return table;
        }

        public async Task<RouletteTable> GetTableAsync(ulong channelId)
        {
            var table = await _cache.HashGetAsync<RouletteTable>(CacheCollection, channelId.ToString());
            if(table == null)
            {
                return null;
            }

            if(table.Bets == null)
            {
                table.Bets = new List<RouletteBet>();
            }

            return table;
        }

        public async Task UpdateTableAsync(ulong channelId, RouletteTable table)
        {
            if(table == null)
            {
                throw new NullReferenceException("table cannot be null");
            }
            await _cache.HashUpsertAsync(CacheCollection, channelId.ToString(), table);
        }
    }
}
