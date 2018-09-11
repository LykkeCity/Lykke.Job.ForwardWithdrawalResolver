using MessagePack;

namespace Lykke.Job.ForwardWithdrawalResolver.Sagas.Events
{
    [MessagePackObject(true)]
    public class CashInProcesedEvent
    {
        public string ClientId { get; set; }

        public string OperationId { get; set; }
    }
}