namespace RagnarokBotWeb.Configuration.Data
{
    public class RateLimitOptions
    {
        public int MaxRequests { get; set; } = 100;
        public TimeSpan TimeWindow { get; set; } = TimeSpan.FromMinutes(1);
    }
}
