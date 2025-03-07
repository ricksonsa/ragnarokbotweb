namespace RagnarokBotWeb.Application.Discord.Dto;

public class ButtonDto(string command, string label)
{
    public string Command { get; } = command;
    public string Label { get; } = label;
}