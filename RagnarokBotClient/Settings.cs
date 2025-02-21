namespace RagnarokBotClient
{
    public class Settings
    {
        public string WebApiUrl { get; set; }

        public Settings(string webApiUrl)
        {
            WebApiUrl = webApiUrl;
        }
    }
}
