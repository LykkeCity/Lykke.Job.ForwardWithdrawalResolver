using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.ForwardWithdrawalResolver.Settings.JobSettings
{
    [UsedImplicitly]
    public class AssetsServiceClientSettings
    {
        [HttpCheck("api/isalive")]
        public string ServiceUrl { set; get; }
    }
}