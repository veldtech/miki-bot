namespace Miki.Services.Transactions
{
    using System;
    using System.Threading.Tasks;

    public interface ITransactionService
    {
        Func<TransactionResponse, Task> TransactionComplete { get; set; }
        Func<TransactionRequest, Exception, Task> TransactionFailed { get; set; }

        Task<TransactionResponse> CreateTransactionAsync(TransactionRequest transaction);
    }
}
