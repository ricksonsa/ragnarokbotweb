namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class ScumServerDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public FtpDto Ftp { get; set; }
        public List<string> RestartTimes { get; set; }
        public DiscordDto? Discord { get; set; }
    }
}
