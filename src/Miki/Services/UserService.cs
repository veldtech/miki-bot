namespace Miki.Services
{
    using System;
    using System.Threading.Tasks;
    using Bot.Models;
    using Bot.Models.Exceptions;
    using Bot.Models.Models.User;
    using Framework;
    using Miki.Discord.Common;
    using Patterns.Repositories;

    public class UserService : IUserService
    {
        private readonly IAsyncRepository<IsBanned> bannedRepository;
        private readonly IAsyncRepository<User> repository;
        private readonly IUnitOfWork unitOfWork;

        public UserService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
            this.repository = unitOfWork.GetRepository<User>();
            this.bannedRepository = unitOfWork.GetRepository<IsBanned>();
        }

        public async ValueTask<User> CreateUserAsync(long userId, string userName)
        {
            var user = new User
            {
                Id = userId,
                DateCreated = DateTime.UtcNow,
                Name = userName,
                MarriageSlots = 1,
            };
            await repository.AddAsync(user);
            await unitOfWork.CommitAsync();
            return user;
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
        public ValueTask UpdateUserAsync(User user)
        {
            return repository.EditAsync(user);
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
        public ValueTask SaveAsync()
        {
            return unitOfWork.CommitAsync();
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }

    public interface IUserService : IDisposable
    {
        ValueTask<User> CreateUserAsync(long userId, string userName);

        ValueTask<User> GetUserAsync(long userId);

        ValueTask UpdateUserAsync(User user);

        ValueTask<bool> UserIsBanned(long userId);

        ValueTask SaveAsync();
    }
}
