using System.Threading.Tasks;
using AzureStorage.Tables;
using Lykke.Job.ForwardWithdrawalResolver.AzureRepositories;
using Lykke.Logs;
using Lykke.SettingsReader.ReloadingManager;
using Xunit;

namespace Lykke.Job.ForwardWithdrawalResolver.Tests
{
    public class UnitTest1
    {
        [Fact(Skip = "manual testing")]
        public async Task Test1()
        {
            string connectionString = "";
            var repo = new BitCoinTransactionsRepository(
                AzureTableStorage<BitCoinTransactionEntity>.Create(
                    ConstantReloadingManager.From(connectionString),
                    "BitCoinTransactions", EmptyLogFactory.Instance));

            var cashout = await repo.ForwardWithdrawalExistsAsync("16935409-504e-4a22-bba0-2f0e11a2019d");
            var forwardWithdrawal = await repo.ForwardWithdrawalExistsAsync("f5018fe1-7950-4353-aeb1-9055b3f6a671");

            Assert.False(cashout);
            Assert.True(forwardWithdrawal);
        }
    }
}
