using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Events.Interactions;

namespace RagnarokBotWeb.Application.Discord.Handlers;

public class InteractionEventHandlerFactory(IServiceProvider serviceProvider) : IInteractionEventHandlerFactory
{
    private readonly Dictionary<string, Func<IInteractionEventHandler>> _handlers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "!welcome_pack", () => new WelcomePackEvent(serviceProvider) }
        };

    public IInteractionEventHandler? GetHandler(SocketInteraction interaction)
    {
        // TODO: check other types of SocketInteraction
        if (interaction is not SocketMessageComponent component) return null;

        return _handlers.TryGetValue(component.Data.CustomId, out var handler) ? handler() : null;
    }
}