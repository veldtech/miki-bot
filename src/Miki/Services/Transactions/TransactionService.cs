namespace Miki.Services
{
    using System;
    using System.Threading.Tasks;
    using Miki.Bot.Models;
    using Miki.Bot.Models.Exceptions;
    using Miki.Framework;
    using Miki.Patterns.Repositories;
    using Miki.Utility;

    public class TransactionService
    {
        public Func<TransactionResponse, Task> TransactionComplete { get; set; }
        public Func<TransactionRequest, Exception, Task> TransactionFailed { get; set; }

        private readonly IUserService service;

        public TransactionService(
            IUserService service)
        {
            this.service = service;
        }

        public Task<TransactionResponse> CreateTransactionAsync(TransactionRequest transaction)
        {
            return service.GetUserAsync(transaction.Receiver)
                .Merge(() => service.GetUserAsync(transaction.Sender))
                .Map(x => TransferAsync(x.Item1, x.Item2, transaction.Amount))
                    .Unwrap()
                    .AndThen(() => service.SaveAsync())
                    .AndThen(CallTransactionComplete)
                .UnwrapErrorAsync(x => CallTransactionFailed(transaction, x));
        }

        public async Task<TransactionResponse> TransferAsync(User receiver, User sender, long amount)
        {
            if(sender.Currency < amount)
            {
                throw new InsufficientCurrencyException(receiver.Currency, amount);
            }

            if(receiver.Id == sender.Id)
            {
                throw new UserNullException();
            }

            sender.Currency -= (int)amount;
            await service.UpdateUserAsync(sender);

            receiver.Currency += (int)amount;
            await service.UpdateUserAsync(receiver);

            return new TransactionResponse(sender, receiver, amount);
        }

        private async Task CallTransactionComplete(TransactionResponse transaction)
        {
            if(TransactionComplete == null)
            {
                return;
            }
            await TransactionComplete(transaction);
        }

        private async Task CallTransactionFailed(TransactionRequest transaction, Exception e)
        {
            if(TransactionFailed == null)
            {
                return;
            }
            await TransactionFailed(transaction, e);
        }
    }

    public class TransactionResponse
    {
        public User Sender { get; }
        public User Receiver { get; }
        public long Amount { get; }

        internal TransactionResponse(User sender, User receiver, long amount)
        {
            Sender = sender;
            Receiver = receiver;
            Amount = amount;
        }

    }

    public class TransactionRequest
    {
        public long Sender { get; }
        public long Receiver { get; }
        public long Amount { get; }

        public TransactionRequest(long sender, long receiver, long amount)
        {
            this.Sender = sender;
            this.Receiver = receiver;
            this.Amount = amount;
        }

        public class Builder
        {
            private Optional<long> sender;
            private Optional<long> receiver;
            private Optional<long> amount;

            public Builder()
            {
                sender = Optional<long>.None;
                receiver = Optional<long>.None;
                amount = Optional<long>.None;
            }
            public TransactionRequest Build()
            {
                if(!sender.HasValue)
                {
                    throw new FailedTransactionException(
                        new ArgumentNullException(nameof(sender)));
                }

                if(!receiver.HasValue)
                {
                    throw new FailedTransactionException(
                        new ArgumentNullException(nameof(receiver)));
                }

                if(!amount.HasValue)
                {
                    throw new FailedTransactionException(
                        new InvalidOperationException("amount cannot be zero"));
                }
                if(amount < 0L)
                {
                    throw new FailedTransactionException(
                        new ArgumentLessThanZeroException());
                }

                return new TransactionRequest(sender, receiver, amount);
            }

            public Builder WithAmount(long amount)
            {
                this.amount = amount;
                return this;
            }

            public Builder WithReceiver(long receiver)
            {
                this.receiver = receiver;
                return this;
            }

            public Builder WithSender(long sender)
            {
                this.sender = sender;
                return this;
            }
        }
    }
}
