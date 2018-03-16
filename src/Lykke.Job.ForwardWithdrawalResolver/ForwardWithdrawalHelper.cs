using System;
using Lykke.Job.ForwardWithdrawalResolver.AzureRepositories;

namespace Lykke.Job.ForwardWithdrawalResolver
{
    public static class ForwardWithdrawalHelper
    {
        public static bool IsDue(this IForwardWithdrawal withdrawal, TimeSpan triggerSpan)
        {
            return DateTime.Now - withdrawal.DateTime > triggerSpan;
        }
    }
}
