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
        private readonly IUnitOfWork unitOfWork;
        private readonly IAsyncRepository<User> repository;
        private readonly IAsyncRepository<IsBanned> bannedRepository;
        private readonly IAsyncRepository<IsDonator> donatorRepository;

        public UserService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
            this.repository = unitOfWork.GetRepository<User>();
            this.bannedRepository = unitOfWork.GetRepository<IsBanned>();
            this.donatorRepository = unitOfWork.GetRepository<IsDonator>();
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
        public async ValueTask<bool> UserIsBannedAsync(long userId)
        {
            var banRecord = await bannedRepository.GetAsync(userId);
            if (banRecord == null)
            {
                return false;
            }
            return banRecord.ExpirationDate > DateTime.UtcNow;
        }

        /// <inheritdoc />
        public async ValueTask<bool> UserIsDonatorAsync(long userId)
        {
            var donatorRecord = await donatorRepository.GetAsync(userId);
            if (donatorRecord == null)
            {
                return false;
            }
            return true;
        }

        /// <inheritdoc />
        public ValueTask UpdateUserAsync(User user)
            => repository.EditAsync(user);

        /// <inheritdoc />
        public ValueTask SaveAsync()
            => unitOfWork.CommitAsync();

        /// <inheritdoc />
        public void Dispose()
            => unitOfWork?.Dispose();
    }

    public interface IUserService : IDisposable
    {
        ValueTask<User> GetUserAsync(long userId);

        ValueTask UpdateUserAsync(User user);

        ValueTask<bool> UserIsBannedAsync(long userId);

        ValueTask<bool> UserIsDonatorAsync(long userId);

        ValueTask SaveAsync();
    }
}
