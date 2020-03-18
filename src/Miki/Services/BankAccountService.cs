namespace Miki.Services
{
    using System;
    using System.Threading.Tasks;
    using Bot.Models;
    using Bot.Models.Exceptions;
    using Bot.Models.Models.User;
    using Framework;
    using Patterns.Repositories;

    public class BankAccountService : IBankAccountService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IAsyncRepository<BankAccount> repository;

        public BankAccountService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
            this.repository = unitOfWork.GetRepository<BankAccount>();
        }

        /// <inheritdoc />
        public async ValueTask<BankAccount> GetOrCreateAccountAsync(long userId, long guildId)
        {
            var account = await repository.GetAsync(userId, guildId);
            if (account == null)
            {
                account = new BankAccount
                {
                    UserId = userId,
                    GuildId = guildId
                };
                await repository.AddAsync(account);
                await unitOfWork.CommitAsync();
            }
            return account;
        }

        /// <inheritdoc />
        public async ValueTask<BankAccount> DepositAsync(long userId, long guildId, int amount)
        {
            var account = await GetOrCreateAccountAsync(userId, guildId).ConfigureAwait(false);

            account.Currency += amount;
            account.TotalDeposited += amount;

            await UpdateAccountAsync(account).ConfigureAwait(false);
            await SaveAsync().ConfigureAwait(false);

            return account;
        }

        /// <inheritdoc />
        public async ValueTask<BankAccount> WithdrawAsync(long userId, long guildId, int amount)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public ValueTask UpdateAccountAsync(BankAccount account)
            => repository.EditAsync(account);

        /// <inheritdoc />
        public ValueTask SaveAsync()
            => unitOfWork.CommitAsync();

        /// <inheritdoc />
        public void Dispose()
            => unitOfWork?.Dispose();
    }

    public interface IBankAccountService : IDisposable
    {
        ValueTask<BankAccount> GetOrCreateAccountAsync(long userId, long guildId);

        ValueTask<BankAccount> DepositAsync(long userId, long guildId, int amount);

        ValueTask<BankAccount> WithdrawAsync(long userId, long guildId, int amount);

        ValueTask UpdateAccountAsync(BankAccount account);

        ValueTask SaveAsync();
    }
}
