using Discord.WebSocket;

namespace RagnarokBotWeb.Application.Discord.Handlers;

public interface IInteractionEventHandler
{
    Task HandleAsync(SocketInteraction interaction);
}