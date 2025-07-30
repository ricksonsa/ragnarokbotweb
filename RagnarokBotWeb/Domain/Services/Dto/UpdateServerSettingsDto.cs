namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class UpdateServerSettingsDto
    {
        public long CoinAwardPeriodically { get; set; }
        public long VipCoinAwardPeriodically { get; set; }
        public List<string> RestartTimes { get; set; }
    }
}
