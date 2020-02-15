namespace Miki.Services
{
    using System.Threading.Tasks;
    using Miki.Bot.Models;
    using Miki.Framework;
    using Miki.Patterns.Repositories;

    // TODO: finish and test GuildService.
    public class GuildService
    {
        private readonly IUnitOfWork unit;
        private readonly IAsyncRepository<GuildUser> guildUserRepository;

        public GuildService(IUnitOfWork unit)
        {
            this.unit = unit;
            guildUserRepository = unit.GetRepository<GuildUser>();
        }

        public async ValueTask<GuildUser> GetGuildAsync(long guildId)
        {
            var guildUser = await guildUserRepository.GetAsync(guildId);
            if(guildUser == null)
            {
                throw new GuildUserNullException();
            }
            return guildUser;
        }

        public async ValueTask<GuildUser> GetRivalAsync(GuildUser user)
        {
            try
            {
                return await GetGuildAsync(user.RivalId);
            }
            catch(GuildUserNullException ex)
            {
                throw new GuildRivalNullException(ex);
            }
        }
    }
}
