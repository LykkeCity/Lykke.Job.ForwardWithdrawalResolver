using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Job.ForwardWithdrawalResolver.Core.Services;

namespace Lykke.Job.ForwardWithdrawalResolver.Services
{
    [UsedImplicitly]
    public class PaymentResolver : IPaymentResolver
    {
        private readonly Dictionary<string, string> _assetMappings;
        private readonly bool _resolveAssetToItselfByDefault;

        public PaymentResolver(
            Dictionary<string, string> assetMappings,
            bool resolveAssetToItselfByDefault)
        {
            _assetMappings = assetMappings;
            _resolveAssetToItselfByDefault = resolveAssetToItselfByDefault;
        }
        
        public Task<string> Resolve(string assetId)
        {
            if (_assetMappings.ContainsKey(assetId))
            {
                return Task.FromResult(_assetMappings[assetId]);
            }

            if (_resolveAssetToItselfByDefault)
            {
                return Task.FromResult(assetId);
            }

            throw new ArgumentException($"Could not resolve {assetId}");
        }
    }
}
