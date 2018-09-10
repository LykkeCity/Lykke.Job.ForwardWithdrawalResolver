using System.Collections.Generic;
using JetBrains.Annotations;

namespace Lykke.Job.ForwardWithdrawalResolver.Settings.JobSettings
{
    [UsedImplicitly]
    public class ForwardWithdrawalResolverSettings
    {
        public DbSettings Db { get; set; }

        public CqrsSettings Cqrs { get; set; }

        public Dictionary<string, string> AssetMappings { set; get; }

        public bool ResolveAssetToItselfByDefault { set; get; }

        public string HotWallet { set; get; }

        public int CriticalDifferenceDays { set; get; }

        public int JobPeriodMinutes { set; get; }
    }
}