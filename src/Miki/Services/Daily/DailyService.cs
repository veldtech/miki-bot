using System;
using System.Threading.Tasks;
using Miki.Bot.Models;
using Miki.Cache;
using Miki.Framework;
using Miki.Patterns.Repositories;
using Miki.Services.Transactions;

namespace Miki.Services.Daily
{
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
        public async ValueTask<DailyResponse> ClaimDailyAsync(long userId)
        {
            var daily = await GetOrCreateDailyAsync(userId).ConfigureAwait(false);

            if(DateTime.UtcNow >= daily.LastClaimTime.AddHours(23))
            {
                if(DateTime.UtcNow < daily.LastClaimTime.AddDays(2))
                {
                    daily.CurrentStreak++;
                    daily.LongestStreak = daily.LongestStreak < daily.CurrentStreak
                        ? daily.CurrentStreak
                        : daily.LongestStreak;
                }
                else
                {
                    daily.CurrentStreak = 0;
                }

                daily.LastClaimTime = DateTime.UtcNow;
                await dailyRepository.EditAsync(daily).ConfigureAwait(false);
                await SaveAsync().ConfigureAwait(false);

                var multiplier = await userService.UserIsDonatorAsync(userId).ConfigureAwait(false)
                    ? 2 : 1;

                var claimAmount = (AppProps.Daily.DailyAmount 
                                   + AppProps.Daily.StreakAmount
                                   * Math.Clamp(daily.CurrentStreak, 0, 100))
                                  * multiplier;

                await transactionService.CreateTransactionAsync(
                    new TransactionRequest.Builder()
                        .WithAmount(claimAmount)
                        .WithReceiver(userId)
                        .WithSender(AppProps.Currency.BankId)
                        .Build());

                return new DailyResponse(daily, DailyStatus.Success, claimAmount);
            }

            return new DailyResponse(daily, DailyStatus.NotReady, 0);
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
        ValueTask<DailyResponse> ClaimDailyAsync(long userId);

        ValueTask<Daily> GetOrCreateDailyAsync(long userId);

        ValueTask UpdateDailyAsync(Daily daily);

        ValueTask SaveAsync();
    }
}
