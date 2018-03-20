using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Common.Log;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.ForwardWithdrawalResolver.AzureRepositories
{
    public class ForwardWithdrawalEntity : TableEntity, IForwardWithdrawal
    {
        public static string GeneratePartitionKey(string clientId)
        {
            return clientId;
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }

        public string Id { get; set; }
        public string AssetId { get; set; }
        public string ClientId { get; set; }
        public double Amount { get; set; }
        public DateTime DateTime { get; set; }
        public string CashInId { get; set; }
    }

    public class ForwardWithdrawalRepository : IForwardWithdrawalRepository
    {
        private readonly INoSQLTableStorage<ForwardWithdrawalEntity> _tableStorage;

        public ForwardWithdrawalRepository(INoSQLTableStorage<ForwardWithdrawalEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<List<IForwardWithdrawal>> GetAllAsync()
        {
            var entities = new List<IForwardWithdrawal>();
            
            await _tableStorage.GetDataByChunksAsync(new TableQuery<ForwardWithdrawalEntity>(),
                enumerable =>
                {
                    lock (entities)
                    {
                        entities.AddRange(enumerable);
                    }
                });

            return entities;
        }

        public async Task<IForwardWithdrawal> TryGetAsync(string clientId, string id)
        {
            return await _tableStorage.GetDataAsync(
                ForwardWithdrawalEntity.GeneratePartitionKey(clientId),
                ForwardWithdrawalEntity.GenerateRowKey(id));
        }

        public Task<bool> DeleteIfExistsAsync(string clientId, string id)
        {
            return _tableStorage.DeleteIfExistAsync(
                ForwardWithdrawalEntity.GeneratePartitionKey(clientId),
                ForwardWithdrawalEntity.GenerateRowKey(id));
        }
    }
}
