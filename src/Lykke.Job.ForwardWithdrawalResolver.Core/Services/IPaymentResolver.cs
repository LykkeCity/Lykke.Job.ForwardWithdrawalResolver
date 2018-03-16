using System;
using System.Threading.Tasks;

namespace Lykke.Job.ForwardWithdrawalResolver.Core.Services
{
    public interface IPaymentResolver
    {
        Task<string> Resolve(string assetId);
    }
}
