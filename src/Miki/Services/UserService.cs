namespace Miki.Services
{
    using System;
    using System.Threading.Tasks;
    using Bot.Models;
    using Bot.Models.Exceptions;
    using Bot.Models.Models.User;
    using Framework;
    using Patterns.Repositories;

    public class UserService : IUserService
    {
        private readonly IAsyncRepository<DailyStreak> dailyStreaksRepository;
        private readonly IAsyncRepository<IsDonator> donatorRepository;
        private readonly IAsyncRepository<IsBanned> bannedRepository;
        private readonly IAsyncRepository<User> repository;
        private readonly IUnitOfWork unitOfWork;

        public UserService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
            this.repository = unitOfWork.GetRepository<User>();
            this.bannedRepository = unitOfWork.GetRepository<IsBanned>();
            this.donatorRepository = unitOfWork.GetRepository<IsDonator>();
            this.dailyStreaksRepository = unitOfWork.GetRepository<DailyStreak>();
            Console.WriteLine("Should be working!");
        }

        /// <inheritdoc />
        public async ValueTask<User> GetUserAsync(long userId)
        {
            var user = await repository.GetAsync(userId);
            if (user == null)
            {
                throw new UserNullException();
            }
            return user;
        }

        /// <inheritdoc />
        public async ValueTask<bool> UserIsBanned(long userId)
        {
            var banRecord = await bannedRepository.GetAsync(userId);
            if (banRecord == null)
            {
                return false;
            }
            return banRecord.ExpirationDate > DateTime.UtcNow;
        }

        /// <inheritdoc />
        public async ValueTask<bool> UserIsDonator(long userId)
        {
            var donatorRecord = await donatorRepository.GetAsync(userId);
            if (donatorRecord == null)
            {
                return false;
            }
            return true;
        }

        /// <inheritdoc />
        public async ValueTask<DailyStreak> GetDailyStreakAsync(long userId)
        {
            var dailyStreakRecord = await dailyStreaksRepository.GetAsync(userId);
            if (dailyStreakRecord == null)
            {
                return null;
            }
            return dailyStreakRecord;
        }

        /// <inheritdoc />
        public ValueTask SaveAsync()
        {
            return unitOfWork.CommitAsync();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            unitOfWork?.Dispose();
        }
    }

    public interface IUserService : IDisposable
    {
        ValueTask<User> GetUserAsync(long userId);

        ValueTask<bool> UserIsBanned(long userId);

        ValueTask<bool> UserIsDonator(long userId);

        ValueTask<DailyStreak> GetDailyStreakAsync(long userId);

        ValueTask SaveAsync();
    }
}
