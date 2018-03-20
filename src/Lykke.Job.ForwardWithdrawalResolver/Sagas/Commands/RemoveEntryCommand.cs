using MessagePack;

namespace Lykke.Job.ForwardWithdrawalResolver.Sagas.Commands
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class RemoveEntryCommand
    {
        public string ClientId { set; get; }
        public string Id { set; get; }
    }
}
