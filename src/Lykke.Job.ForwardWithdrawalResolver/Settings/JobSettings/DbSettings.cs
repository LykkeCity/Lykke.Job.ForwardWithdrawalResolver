using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.ForwardWithdrawalResolver.Settings.JobSettings
{
    [UsedImplicitly]
    public class DbSettings
    {
        [AzureTableCheck]
        public string LogsConnString { get; set; }
        [AzureTableCheck]
        public string ForwardWithdrawalsConnString { get; set; }
    }
}
