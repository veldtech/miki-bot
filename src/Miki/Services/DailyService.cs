using ProtoBuf;

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
        private readonly IAsyncRepository<User> userRepository;
        private readonly IAsyncRepository<Daily> dailyStreakRepository;

        public DailyService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
            this.userRepository = unitOfWork.GetRepository<User>();
            this.dailyStreakRepository = unitOfWork.GetRepository<Daily>();
        }

        /// <inheritdoc />
        public async ValueTask ClaimDailyAsync(ulong userId)
        {
            var user = await userRepository.GetAsync(userId);
            
        }

        /// <inheritdoc />
        public async ValueTask<Daily> GetStreakAsync(ulong userId)
             => await dailyStreakRepository.GetAsync(userId);

        /// <inheritdoc />
        public ValueTask UpdateStreakAsync(Daily dailyStreak)
            => dailyStreakRepository.EditAsync(dailyStreak);

        /// <inheritdoc />
        public ValueTask SaveAsync()
            => unitOfWork.CommitAsync();

        /// <inheritdoc />
        public void Dispose()
            => unitOfWork.Dispose();
    }

    public class DailyClaimResponse
    {
        public bool claimSuccess;
        //public  failReason;
    }

    public interface IDailyService : IDisposable
    {
        ValueTask ClaimDailyAsync(ulong userId);

        ValueTask<Daily> GetStreakAsync(ulong userId);

        ValueTask UpdateStreakAsync(Daily dailyStreak);

        ValueTask SaveAsync();
    }
}
