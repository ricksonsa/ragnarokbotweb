namespace RagnarokBotWeb.Application.Discord.Dto;

public class ButtonDto(string command, string label, ulong messageId)
{
    public string Command { get; } = command;
    public string Label { get; } = label;
    public ulong MessageId { get; set; } = messageId;
}