using System;
using System.Threading.Tasks;

namespace Miki.Services.Transactions
{
    public interface ITransactionService
    {
        Task<TransactionResponse> CreateTransactionAsync(TransactionRequest transaction);
    }
}
