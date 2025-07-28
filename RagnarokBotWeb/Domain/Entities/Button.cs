namespace RagnarokBotWeb.Domain.Entities;

public class Button : BaseEntity
{
    public string Label { get; set; }
    public string Command { get; set; }
    public Channel Channel { get; set; }
    public ulong MessageId { get; set; }


    public Button(string command, string label, ulong messageId)
    {
        Label = label;
        Command = command;
        MessageId = messageId;
    }
}