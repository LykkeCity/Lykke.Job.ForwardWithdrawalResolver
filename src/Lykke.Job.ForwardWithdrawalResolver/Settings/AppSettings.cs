using JetBrains.Annotations;
using Lykke.Job.ForwardWithdrawalResolver.Settings.JobSettings;
using Lykke.Job.ForwardWithdrawalResolver.Settings.SlackNotifications;
using Lykke.Service.ExchangeOperations.Client;

namespace Lykke.Job.ForwardWithdrawalResolver.Settings
{
    [UsedImplicitly]
    public class AppSettings
    {
        public ForwardWithdrawalResolverSettings ForwardWithdrawalResolverJob { get; set; }
        public ExchangeOperationsServiceClientSettings ExchangeOperationsServiceClient { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
    }
}
