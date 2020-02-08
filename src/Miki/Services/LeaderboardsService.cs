namespace Miki.Services
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Miki.Bot.Models;
    using Miki.Bot.Models.Exceptions;
    using Miki.Bot.Models.Queries;
    using Miki.Cache;
    using Miki.Framework;
    using Miki.Patterns.Repositories;

    public class LeaderboardsService
    {
        private readonly IAsyncRepository<RankObject> globalRankView;
        private readonly IAsyncRepository<LocalExperience> localRepository;
        private readonly ICacheClient cache;
        private readonly DbContext ctx;

        public LeaderboardsService(DbContext ctx, IUnitOfWork unit, ICacheClient cache)
        {
            this.ctx = ctx;
            this.cache = cache;
            globalRankView = new EntityRepository<RankObject>(ctx);
            localRepository = unit.GetRepository<LocalExperience>();
        }

        public async Task<int> GetLocalRankAsync(long guildId, Expression<Func<LocalExperience, bool>> where)
        {
            return await (await localRepository.ListAsync()).AsQueryable()
                .Where(x => x.ServerId == guildId)
                .Where(where)
                .CountAsync();
        }

        public async Task<int?> GetGlobalRankAsync(long userId)
        {
            if(await ShouldRefreshGlobalViewAsync())
            {
                await RefreshGlobalViewAsync();
            }

            var rankObject = await (await globalRankView.ListAsync())
                .AsQueryable()
                .FirstOrDefaultAsync(x => x.Id == userId);
            return rankObject?.Rank;
        }

        private async Task RefreshGlobalViewAsync()
        {
            // TODO: remove hack from here.
            await ctx.Database.ExecuteSqlInterpolatedAsync(
                $"REFRESH MATERIALIZED VIEW CONCURRENTLY dbo.\"mview_glob_rank_exp\" WITH DATA");
            await ctx.SaveChangesAsync();
            await cache.UpsertAsync(GetCacheKey(), 1, TimeSpan.FromMinutes(15));
        }

        private async Task<bool> ShouldRefreshGlobalViewAsync()
        {
            return (await cache.GetAsync<int>(GetCacheKey())) == 1;
        }

        private string GetCacheKey()
        {
            return "leaderboards:global";
        }

        public class Config
        {}
    }
}
