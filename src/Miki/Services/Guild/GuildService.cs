using System;
using System.Threading.Tasks;
using Miki.Bot.Models;
using Miki.Framework;
using Miki.Patterns.Repositories;
using Miki.Services.Transactions;
using Miki.Utility;

namespace Miki.Services
{
    // TODO: finish and test GuildService.
    public class GuildService : IGuildService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IAsyncRepository<GuildUser> guildUserRepository;
        private readonly IAsyncRepository<Timer> timerRepository;
        private readonly IAsyncRepository<LocalExperience> localExperienceRepository;

        private readonly ITransactionService transactionService;

        public GuildService(
            IUnitOfWork unit,
            ITransactionService transactionService)
        {
            this.unitOfWork = unit;
            guildUserRepository = unit.GetRepository<GuildUser>();
            timerRepository = unit.GetRepository<Timer>();
            localExperienceRepository = unit.GetRepository<LocalExperience>();

            this.transactionService = transactionService;
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

        public async ValueTask<GuildUser> GetRivalAsync(GuildUser guildUser)
        {
            try
            {
                return await GetGuildAsync(guildUser.RivalId);
            }
            catch(GuildUserNullException)
            {
                throw new GuildRivalNullException();
            }
        }

        /// <inheritdoc />
        public async ValueTask<LocalExperience> CreateLocalExperienceAsync(GuildUserReference guildUser)
        {
            var localExperience = new LocalExperience
            {
                ServerId = guildUser.GuildId,
                UserId = guildUser.UserId,
                Experience = 0
            };
            await localExperienceRepository.AddAsync(localExperience);
            await unitOfWork.CommitAsync();
            return localExperience;
        }

        /// <inheritdoc />
        public async ValueTask<LocalExperience> GetLocalExperienceAsync(GuildUserReference guildUser)
        {
            var localExperience = await localExperienceRepository.GetAsync(guildUser.GuildId, guildUser.UserId);
            if (localExperience == null)
            {
                throw new LocalExperienceNullException();
            }
            return localExperience;
        }

        /// <inheritdoc />
        public async ValueTask<Timer> CreateTimerAsync(GuildUserReference guildUser)
        {
            var timer = new Timer
            {
                GuildId = guildUser.GuildId,
                UserId = guildUser.UserId,
                Value = DateTime.UtcNow.AddDays(-30)
            };
            await timerRepository.AddAsync(timer);
            await unitOfWork.CommitAsync();
            return timer;
        }

        /// <inheritdoc />
        public async ValueTask<Timer> GetTimerAsync(GuildUserReference guildUserReference)
        {
            var timer = await timerRepository.GetAsync(guildUserReference.GuildId, guildUserReference.UserId);
            if (timer == null)
            {
                throw new TimerNullException();
            }
            return timer;
        }

        /// <inheritdoc />
        public async ValueTask<WeeklyResponse> ClaimWeeklyAsync(GuildUserReference guildUserReference)
        {
            var guild = await GetGuildAsync(guildUserReference.GuildId);
            var rival = await GetRivalAsync(guild);

            if (rival.Experience > guild.Experience)
            {
                return new WeeklyResponse(
                    WeeklyStatus.GuildInsufficientExp,
                    0,
                    DateTime.Now);
            }

            var localExperience = await this.GetOrCreateLocalExperienceAsync(guildUserReference);

            if (localExperience.Experience < guild.MinimalExperienceToGetRewards)
            {
                return new WeeklyResponse(
                    WeeklyStatus.UserInsufficientExp, 
                    0, 
                    DateTime.Now, 
                    guild.MinimalExperienceToGetRewards - localExperience.Experience);
            }

            var timer = await this.GetOrCreateTimerAsync(guildUserReference);

            if (timer.Value.AddDays(7) > DateTime.UtcNow)
            {
                return new WeeklyResponse(
                    WeeklyStatus.NotReady,
                    0,
                    timer.Value);
            }

            var amountClaimed = (int)Math.Round(
                (MikiRandom.NextDouble() + guild.GuildHouseMultiplier)
                * 0.5 * 10 * guild.CalculateLevel(guild.Experience));

            await transactionService.CreateTransactionAsync(
                new TransactionRequest.Builder()
                    .WithAmount(amountClaimed)
                    .WithReceiver(guildUserReference.UserId)
                    .WithSender(AppProps.Currency.BankId)
                    .Build());

            timer.Value = DateTime.UtcNow;

            await UpdateTimerAsync(timer);
            await SaveAsync();

            return new WeeklyResponse(WeeklyStatus.Success, amountClaimed, timer.Value);
        }

        /// <inheritdoc />
        public ValueTask UpdateTimerAsync(Timer timer)
            => timerRepository.EditAsync(timer);

        /// <inheritdoc />
        public ValueTask UpdateLocalExperienceAsync(LocalExperience localExperience)
            => localExperienceRepository.EditAsync(localExperience);

        public ValueTask SaveAsync()
            => unitOfWork.CommitAsync();

        /// <inheritdoc />
        public void Dispose()
            => unitOfWork.Dispose();
    }

    public struct GuildUserReference
    {
        public long GuildId { get; }
        public long UserId { get; }

        public GuildUserReference(long guildId, long userId)
        {
            GuildId = guildId;
            UserId = userId;
        }
    }

    public interface IGuildService : IDisposable
    {
        ValueTask<GuildUser> GetGuildAsync(long guildId);

        ValueTask<GuildUser> GetRivalAsync(GuildUser guildUser);

        ValueTask<LocalExperience> CreateLocalExperienceAsync(GuildUserReference guildUser);

        ValueTask<LocalExperience> GetLocalExperienceAsync(GuildUserReference guildUser);

        ValueTask<Timer> CreateTimerAsync(GuildUserReference guildUser);

        ValueTask<Timer> GetTimerAsync(GuildUserReference guildUser);

        ValueTask<WeeklyResponse> ClaimWeeklyAsync(GuildUserReference guildUserReference);

        ValueTask UpdateTimerAsync(Timer timer);

        ValueTask UpdateLocalExperienceAsync(LocalExperience localExperience);

        ValueTask SaveAsync();
    }
}
