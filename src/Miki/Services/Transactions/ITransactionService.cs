namespace Miki.Services.Transactions
{
    using System.Threading.Tasks;

    public interface ITransactionService
    {
        Task<TransactionResponse> CreateTransactionAsync(TransactionRequest transaction);
    }
}
