namespace Miki.Services
{
    using System;
    using System.Threading.Tasks;

    using Bot.Models;

    using Framework;

    using Patterns.Repositories;

    public class StreakService : IStreakService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IAsyncRepository<Daily> repository;

        public StreakService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
            this.repository = unitOfWork.GetRepository<Daily>();
        }

        /// <inheritdoc />
        public async ValueTask<Daily> GetStreakAsync(long userId)
        {
            var dailyStreakRecord = await repository.GetAsync(userId);
            if (dailyStreakRecord == null)
            {
                return null;
            }
            return dailyStreakRecord;
        }

        /// <inheritdoc />
        public ValueTask UpdateStreakAsync(Daily dailyStreak)
            => repository.EditAsync(dailyStreak);

        /// <inheritdoc />
        public ValueTask SaveAsync()
            => unitOfWork.CommitAsync();

        /// <inheritdoc />
        public void Dispose()
            => unitOfWork?.Dispose();
    }

    public interface IStreakService : IDisposable
    {
        ValueTask<Daily> GetStreakAsync(long userId);

        ValueTask UpdateStreakAsync(Daily dailyStreak);

        ValueTask SaveAsync();
    }
}
