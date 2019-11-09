namespace Miki.Services
{
    using System;
    using System.Threading.Tasks;
    using Bot.Models;
    using Bot.Models.Exceptions;
    using Framework;
    using Patterns.Repositories;

    public class UserService : IUserService
    {
        private readonly IAsyncRepository<User> repository;
        private readonly IUnitOfWork unitOfWork;

        public UserService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
            this.repository = unitOfWork.GetRepository<User>();
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

        ValueTask SaveAsync();
    }
}
