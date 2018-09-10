using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Job.ForwardWithdrawalResolver.AzureRepositories
{
    public interface IForwardWithdrawal
    {
        string Id { get; }
        string AssetId { get; }
        string ClientId { get; }
        double Amount { get; }
        DateTime DateTime { get; }
        string CashInId { get; }
        string CashOutId { get; }
        DateTimeOffset Timestamp { get; }
    }

    public class ForwardWithdrawal : IForwardWithdrawal
    {
        public string Id { get; set; }
        public string AssetId { get; set; }
        public string ClientId { get; set; }
        public double Amount { get; set; }
        public DateTime DateTime { get; set; }
        public string CashInId { get; set; }
        public string CashOutId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

    public interface IForwardWithdrawalRepository
    {
        Task<List<IForwardWithdrawal>> GetAllAsync();
        Task<IForwardWithdrawal> TryGetAsync(string clientId, string id);
        Task<IForwardWithdrawal> TryGetByCashoutIdAsync(string clientId, string cashoutId);
        Task<bool> DeleteIfExistsAsync(string clientId, string id);
    }
}