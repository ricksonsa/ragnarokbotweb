namespace RagnarokBotWeb.Domain.Services.Dto;

public class DiscordDto
{
    public string Token { get; set; }
    public bool Confirmed { get; set; }
    public string DiscordLink { get; set; }
    public string Name { get; set; }
    public ulong Id { get; set; }
}