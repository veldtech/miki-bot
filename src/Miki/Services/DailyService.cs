using System.ComponentModel.DataAnnotations;
using Miki.Cache;
using Miki.Core.Migrations;
using Miki.Logging;
using Miki.Services.Transactions;
using Miki.Utility;

namespace Miki.Services
{
    using System;
    using System.Threading.Tasks;

    using Bot.Models;

    using Framework;

    using Patterns.Repositories;

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
                    var cache = context.GetService<ICacheClient>();
                    var redisKey = $"user:{userId}:daily";

                    if (await cache.ExistsAsync(redisKey).ConfigureAwait(false))
                    {
                        daily.CurrentStreak = await cache.GetAsync<int>(redisKey).ConfigureAwait(false);
                        await cache.RemoveAsync(redisKey);
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

    public class DailyClaimResponse
    {
        public DailyStatus Status { get; set; }
        public int AmountClaimed { get; set; }
        public int LongestStreak { get; set; }
        public int CurrentStreak { get; set; }
        public DateTime LastClaimTime { get; set; }

        public DailyClaimResponse(Daily daily, DailyStatus status, int amountClaimed)
        {
            Status = status;
            AmountClaimed = amountClaimed;
            LongestStreak = daily.LongestStreak;
            CurrentStreak = daily.CurrentStreak;
            LastClaimTime = daily.LastClaimTime;
        }
    }

    public enum DailyStatus
    {
        Success,
        Claimed
    }

    public interface IDailyService : IDisposable
    {
        ValueTask<DailyClaimResponse> ClaimDailyAsync(long userId, IContext context);

        ValueTask<Daily> GetOrCreateDailyAsync(long userId);

        ValueTask UpdateDailyAsync(Daily daily);

        ValueTask SaveAsync();
    }
}
