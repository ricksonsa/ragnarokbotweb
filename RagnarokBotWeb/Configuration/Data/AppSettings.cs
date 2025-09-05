namespace RagnarokBotWeb.Configuration.Data
{
    public class AppSettings
    {
        public static bool IsDevelopment { get; set; }
        public string DefaultCron { get; set; }
        public string FiveMinCron { get; set; }
        public string TwoMinCron { get; set; }
        public string TwentySecondsCron { get; set; }
        public string BaseUrl { get; set; }
        public string PayPalUrl { get; set; }
        public int SocketServerPort { get; set; }
        public string DiscordToken { get; set; }
        public string DiscordInstallLink { get; set; }
    }

    public class SecuritySettings
    {
        public bool UseHsts { get; set; }
        public bool UseHttpsRedirection { get; set; }
        public bool UseRateLimiting { get; set; }
        public RateLimitOptions? RateLimit { get; set; }
        public Cors? Cors { get; set; }
    }

    public class Cors
    {
        public string AllowedOrigins { get; set; }
    }

    public static class AppSettingsStatic
    {
        public static string DefaultCron { get; set; } = "0 0/2 * * * ?";
        public static string FiveMinCron { get; set; } = "0 0/5 * * * ?";
        public static string TenMinCron { get; set; } = "0 0/10 * * * ?";
        public static string OneMinCron { get; set; } = "0 0/1 * * * ?";
        public static string TwoMinCron { get; set; } = "0 0/2 * * * ?";
        public static string TenSecondsCron { get; set; } = "0/10 * * * * ?";
        public static string ThirtySecondsCron { get; set; } = "0/30 * * * * ?";
        public static string EveryDayCron { get; set; } = "0 0 * * * ?";
    }
}
