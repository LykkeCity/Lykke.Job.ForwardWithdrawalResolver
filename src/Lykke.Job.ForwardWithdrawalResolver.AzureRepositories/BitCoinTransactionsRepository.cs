using System.Threading.Tasks;
using AzureStorage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Job.ForwardWithdrawalResolver.AzureRepositories
{
    public class BitCoinTransactionEntity : TableEntity
    {
        public string TransactionId => RowKey;

        private static string GeneratePartitionKey() => "TransId";

        private static string GenerateRowKey(string transactionId) => transactionId;

        public static BitCoinTransactionEntity Create(string id)
        {
            return new BitCoinTransactionEntity
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(id)
            };
        }
    }

    public class BitCoinTransactionsRepository : IBitCoinTransactionsRepository
    {
        private readonly INoSQLTableStorage<BitCoinTransactionEntity> _tableStorage;

        public BitCoinTransactionsRepository(INoSQLTableStorage<BitCoinTransactionEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task<bool> TransactionExistsAsync(string transactionId)
        {
            var entity = BitCoinTransactionEntity.Create(transactionId);

            return _tableStorage.RecordExistsAsync(entity);
        }
    }
}
