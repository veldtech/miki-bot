namespace Miki.Services.Daily
{
    using System;
    using System.Threading.Tasks;
    using Bot.Models;
    using Cache;
    using Framework;
    using Patterns.Repositories;
    using Transactions;

    public class DailyService : IDailyService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IAsyncRepository<Daily> dailyRepository;

        private readonly IUserService userService;
        private readonly ITransactionService transactionService;

        public DailyService(IUnitOfWork unitOfWork, IUserService userService, ITransactionService transactionService)
        {
            this.unitOfWork = unitOfWork;
            dailyRepository = unitOfWork.GetRepository<Daily>();

            this.userService = userService;
            this.transactionService = transactionService;
        }

        /// <inheritdoc />
        public async ValueTask<DailyClaimResponse> ClaimDailyAsync(long userId)
            => await ClaimDailyAsync(userId, null);

        /// <inheritdoc />
        public async ValueTask<DailyClaimResponse> ClaimDailyAsync(long userId, IContext context)
        {
            var daily = await GetOrCreateDailyAsync(userId).ConfigureAwait(false);
            await dailyRepository.EditAsync(daily).ConfigureAwait(false);

            if (DateTime.UtcNow >= daily.LastClaimTime.AddHours(23))
            {
                /*
                 * Temporary code for transferring streaks from cache.
                 */
                if (context != null)
                {
                    var redisKey = $"user:{userId}:daily";
                    var cacheClient = context.GetService<ICacheClient>();
                    var cacheExists = await cacheClient.ExistsAsync(redisKey).ConfigureAwait(false);

                    if (cacheExists)
                    {
                        daily.CurrentStreak = await cacheClient.GetAsync<int>(redisKey).ConfigureAwait(false);
                        await cacheClient.RemoveAsync(redisKey);
                    }
                }
                /*
                 * End of temporary code.
                 */

                if (DateTime.UtcNow < daily.LastClaimTime.AddDays(2))
                {
                    daily.CurrentStreak++;
                    daily.LongestStreak = daily.LongestStreak < daily.CurrentStreak ? daily.CurrentStreak : daily.LongestStreak;
                }
                else
                {
                    daily.CurrentStreak = 1;
                }

                daily.LastClaimTime = DateTime.UtcNow;

                await SaveAsync().ConfigureAwait(false);

                var multiplier = await userService.UserIsDonatorAsync(userId).ConfigureAwait(false) ? 2 : 1;
                var claimAmount = (AppProps.Daily.DailyAmount + AppProps.Daily.StreakAmount * daily.CurrentStreak) * multiplier;

                await transactionService.CreateTransactionAsync(
                    new TransactionRequest.Builder()
                        .WithAmount(claimAmount)
                        .WithReceiver(userId)
                        .WithSender(AppProps.Currency.BankId)
                        .Build());

                return new DailyClaimResponse(daily, DailyStatus.Success, claimAmount);
            }
            else
            {
                return new DailyClaimResponse(daily, DailyStatus.Claimed, 0);
            }
        }

        /// <inheritdoc />
        public async ValueTask<Daily> GetOrCreateDailyAsync(long userId)
        {
            var daily = await dailyRepository.GetAsync(userId);
            if (daily == null)
            {
                daily = new Daily
                {
                    UserId = userId,
                    LastClaimTime = DateTime.UtcNow.AddDays(-1)
                };
                await dailyRepository.AddAsync(daily);
                await unitOfWork.CommitAsync();
            }
            return daily;
        }

        /// <inheritdoc />
        public ValueTask UpdateDailyAsync(Daily daily)
            => dailyRepository.EditAsync(daily);

        /// <inheritdoc />
        public ValueTask SaveAsync()
            => unitOfWork.CommitAsync();

        /// <inheritdoc />
        public void Dispose()
            => unitOfWork.Dispose();
    }

    public interface IDailyService : IDisposable
    {
        ValueTask<DailyClaimResponse> ClaimDailyAsync(long userId, IContext context);

        ValueTask<Daily> GetOrCreateDailyAsync(long userId);

        ValueTask UpdateDailyAsync(Daily daily);

        ValueTask SaveAsync();
    }
}
