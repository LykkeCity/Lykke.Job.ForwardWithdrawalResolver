using System;
using Lykke.Job.ForwardWithdrawalResolver.AzureRepositories;

namespace Lykke.Job.ForwardWithdrawalResolver
{
    public static class ForwardWithdrawalHelper
    {
        public static bool IsDue(this IForwardWithdrawal withdrawal, TimeSpan triggerSpan)
        {
            return DateTime.UtcNow - withdrawal.DateTime > triggerSpan;
        }
        
        //This method is used to raise awareness in case difference between DateTime and Timestamp is too big
        public static bool DateTimeTimestampDifferenceTooBig(this IForwardWithdrawal withdrawal, TimeSpan critical)
        {
            return withdrawal.Timestamp.UtcDateTime - withdrawal.DateTime > critical;
        }
    }
}
