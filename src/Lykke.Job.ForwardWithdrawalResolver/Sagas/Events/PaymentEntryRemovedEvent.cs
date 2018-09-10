using MessagePack;

namespace Lykke.Job.ForwardWithdrawalResolver.Sagas.Events
{
    [MessagePackObject(true)]
    public class PaymentEntryRemovedEvent
    {
        public string Id { set; get; }
        public string ClientId { set; get; }
        public string AssetId { set; get; }
        public double Amount { set; get; }
        public string CashInId { set; get; }
    }
}