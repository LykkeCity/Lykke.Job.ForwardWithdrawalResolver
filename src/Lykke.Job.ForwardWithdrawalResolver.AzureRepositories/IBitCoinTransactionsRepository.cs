using System.Threading.Tasks;

namespace Lykke.Job.ForwardWithdrawalResolver.AzureRepositories
{
    public interface IBitCoinTransactionsRepository
    {
        Task<bool> ForwardWithdrawalExistsAsync(string transactionId);
    }
}
