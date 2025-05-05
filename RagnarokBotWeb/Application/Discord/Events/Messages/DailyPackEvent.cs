using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Handlers;

namespace RagnarokBotWeb.Application.Discord.Events.Messages;

public class DailyPackEvent : IMessageEventHandler
{
    public Task HandleAsync(SocketMessage message)
    {
        return message.Channel.SendMessageAsync("Daily Pack Event");
    }

    public Task HandleAsync(SocketMessageComponent message)
    {
        return message.Channel.SendMessageAsync("Daily Pack Event");
    }
}