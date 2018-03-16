using System.Collections.Generic;
using JetBrains.Annotations;

namespace Lykke.Job.ForwardWithdrawalResolver.Settings.JobSettings
{
    [UsedImplicitly]
    public class ForwardWithdrawalResolverSettings
    {
        public DbSettings Db { get; set; }
        public RabbitMqSettings Rabbit { get; set; }
        public Dictionary<string, string> AssetMappings { set; get; }
        public bool ResolveAssetToItselfByDefault { set; get; }
        public string HotWallet { set; get; }
        public int DaysToTrigger { set; get; }
    }
}
