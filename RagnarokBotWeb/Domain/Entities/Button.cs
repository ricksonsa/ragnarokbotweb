namespace RagnarokBotWeb.Domain.Entities;

public class Button : BaseEntity
{
    public string Label { get; set; }
    public string Command { get; set; }
    public Channel Channel { get; set; }
}