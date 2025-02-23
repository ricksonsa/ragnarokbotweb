using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Handlers;

namespace RagnarokBotWeb.Application.Discord.Events;

public class DailyPackEvent : IMessageEventHandler
{
    public Task HandleAsync(SocketMessage message)
    {
        return message.Channel.SendMessageAsync("Daily Pack Event");
    }
}