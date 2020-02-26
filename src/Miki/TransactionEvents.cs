namespace Miki
{
    using System;
    using System.Threading.Tasks;
    using Miki.Services.Transactions;

    public class TransactionEvents
    {
        public Func<TransactionResponse, Task> OnTransactionComplete { get; set; }
        public Func<TransactionRequest, Exception, Task> OnTransactionFailed { get; set; }

        public async Task CallTransactionCompleted(TransactionResponse r)
        {
            if(OnTransactionComplete != null)
            {
                await OnTransactionComplete(r);
            }
        }

        public async Task CallTransactionFailed(TransactionRequest request, Exception ex)
        {
            if(OnTransactionFailed != null)
            {
                await OnTransactionFailed(request, ex);
            }
        }
    }
}