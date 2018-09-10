using JetBrains.Annotations;
using Lykke.Job.ForwardWithdrawalResolver.Settings.JobSettings;
using Lykke.Job.OperationsCache.Client;
using Lykke.Sdk.Settings;
using Lykke.Service.ExchangeOperations.Client;
using Lykke.Service.OperationsHistory.Client;

namespace Lykke.Job.ForwardWithdrawalResolver.Settings
{
    [UsedImplicitly]
    public class AppSettings : BaseAppSettings
    {
        public ForwardWithdrawalResolverSettings ForwardWithdrawalResolverJob { get; set; }
        public AssetsServiceClientSettings AssetsServiceClient { get; set; }
        public OperationsHistoryServiceClientSettings OperationsHistoryServiceClient { get; set; }
        public OperationsCacheServiceClientSettings OperationsCacheJobClient { get; set; }
        public ExchangeOperationsServiceClientSettings ExchangeOperationsServiceClient { get; set; }
    }
}