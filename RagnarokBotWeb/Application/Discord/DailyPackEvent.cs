using Discord.WebSocket;

namespace RagnarokBotWeb.Application.Discord;

public class DailyPackEvent : IMessageEventHandler
{
    public Task HandleAsync(SocketMessage message)
    {
        return message.Channel.SendMessageAsync("Daily Pack Event");
    }
}