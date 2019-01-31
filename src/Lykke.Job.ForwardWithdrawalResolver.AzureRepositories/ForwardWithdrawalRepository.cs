using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.ForwardWithdrawalResolver.AzureRepositories
{
    public class ForwardWithdrawalEntity : TableEntity, IForwardWithdrawal
    {
        public string Id { get; set; }
        public string AssetId { get; set; }
        public string ClientId { get; set; }
        public double Amount { get; set; }
        public DateTime DateTime { get; set; }
        public string CashInId { get; set; }
        public string CashOutId { get; set; }

        public static string GeneratePartitionKey(string clientId)
        {
            return clientId;
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }
    }

    public class ForwardWithdrawalRepository : IForwardWithdrawalRepository
    {
        private readonly INoSQLTableStorage<ForwardWithdrawalEntity> _tableStorage;

        public ForwardWithdrawalRepository(INoSQLTableStorage<ForwardWithdrawalEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task ProcessByChunksAsync(Func<IEnumerable<IForwardWithdrawal>, Task> processHandler)
        {
            await _tableStorage.GetDataByChunksAsync(new TableQuery<ForwardWithdrawalEntity>(), processHandler);
        }

        public async Task<IForwardWithdrawal> TryGetAsync(string clientId, string id)
        {
            return await _tableStorage.GetDataAsync(
                ForwardWithdrawalEntity.GeneratePartitionKey(clientId),
                ForwardWithdrawalEntity.GenerateRowKey(id));
        }

        public async Task<IForwardWithdrawal> TryGetByCashoutIdAsync(string clientId, string cashoutId)
        {
            var byClientId = await _tableStorage.GetDataAsync(ForwardWithdrawalEntity.GeneratePartitionKey(clientId));

            return byClientId.FirstOrDefault(x => x.CashOutId == cashoutId);
        }

        public Task<bool> DeleteIfExistsAsync(string clientId, string id)
        {
            return _tableStorage.DeleteIfExistAsync(
                ForwardWithdrawalEntity.GeneratePartitionKey(clientId),
                ForwardWithdrawalEntity.GenerateRowKey(id));
        }
    }
}