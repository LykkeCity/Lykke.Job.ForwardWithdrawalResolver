﻿using MessagePack;

namespace Lykke.Job.ForwardWithdrawalResolver.Sagas.Commands
{
    [MessagePackObject(true)]
    public class RemoveEntryFromHistoryJobCommand
    {
        public string Id { set; get; }
        public string ClientId { set; get; }
        public string AssetId { set; get; }
        public double Amount { set; get; }
        public string CashInId { set; get; }
    }
}