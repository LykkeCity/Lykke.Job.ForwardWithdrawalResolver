using System.Threading.Tasks;

namespace Lykke.Job.ForwardWithdrawalResolver.AzureRepositories
{
    public interface IBitCoinTransactionsRepository
    {
        Task<bool> TransactionExistsAsyncAsync(string transactionId);
    }
}
