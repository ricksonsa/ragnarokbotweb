using Discord.WebSocket;

namespace RagnarokBotWeb.Application.Discord.Handlers;

public interface IInteractionEventHandlerFactory
{
    IInteractionEventHandler? GetHandler(SocketInteraction interaction);
}