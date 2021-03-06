﻿using System;
using MessagePack;

namespace Lykke.Job.ForwardWithdrawalResolver.Sagas.Commands
{
    [MessagePackObject(true)]
    public class ProcessPaymentCommand
    {
        public string Id { set; get; }
        public string ClientId { set; get; }
        public string AssetId { set; get; }
        public double Amount { set; get; }
        public Guid NewCashinId { get; set; }
        public string CashinId { get; set; }
    }
}