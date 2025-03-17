namespace RagnarokBotWeb.Configuration.Data
{
    public class AppSettings
    {
        public string DefaultCron { get; set; }
        public string FiveMinCron { get; set; }
        public string TwoMinCron { get; set; }
        public string TwentySecondsCron { get; set; }
    }

    public static class AppSettingsStatic
    {
        public static string DefaultCron { get; set; } = "0 0/1 * * * ?";
        public static string FiveMinCron { get; set; } = "0 0/5 * * * ?";
        public static string TenMinCron { get; set; } = "0 0/10 * * * ?";
        public static string TwoMinCron { get; set; } = "0 0/2 * * * ?";
        public static string TwentySecondsCron { get; set; } = "0/20 * * * * ?";
    }
}
