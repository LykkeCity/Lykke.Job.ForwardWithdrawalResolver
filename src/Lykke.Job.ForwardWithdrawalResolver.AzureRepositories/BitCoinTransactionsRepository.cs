using System.Threading.Tasks;
using AzureStorage;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;

namespace Lykke.Job.ForwardWithdrawalResolver.AzureRepositories
{
    public class BitCoinTransactionEntity : AzureTableEntity
    {
        public string TransactionId => RowKey;

        [JsonValueSerializer]
        public ContextData ContextData { get; set; }

        internal static string GeneratePartitionKey() => "TransId";
        internal static string GenerateRowKey(string transactionId) => transactionId;
    }

    public class ContextData
    {
        [JsonValueSerializer]
        public AdditionalData AddData { get; set; }
    }

    public class AdditionalData
    {
        [JsonValueSerializer]
        public ForwardWithdrawalData ForwardWithdrawal { get; set; }
    }

    public class ForwardWithdrawalData
    {
        public string Id { get; set; }
    }

    public class BitCoinTransactionsRepository : IBitCoinTransactionsRepository
    {
        private readonly INoSQLTableStorage<BitCoinTransactionEntity> _tableStorage;

        public BitCoinTransactionsRepository(INoSQLTableStorage<BitCoinTransactionEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<bool> ForwardWithdrawalExistsAsync(string transactionId)
        {
            var entity = await _tableStorage.GetDataAsync(BitCoinTransactionEntity.GeneratePartitionKey(), BitCoinTransactionEntity.GenerateRowKey(transactionId));

            return entity?.ContextData?.AddData?.ForwardWithdrawal != null;
        }
    }
}
