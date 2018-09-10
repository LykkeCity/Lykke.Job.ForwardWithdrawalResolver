using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.ForwardWithdrawalResolver.Settings.JobSettings
{
    public class CqrsSettings
    {
        [AmqpCheck]
        public string RabbitConnString { get; set; }
    }
}