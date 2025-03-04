using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Handlers;

namespace RagnarokBotWeb.Application.Discord.Events.Interaction;

public class WelcomePackEvent : IInteractionEventHandler
{
    public async Task HandleAsync(SocketInteraction interaction)
    {
        if (interaction is not SocketMessageComponent component) return;

        var user = component.User as SocketGuildUser;

        try
        {
            var dmChannel = await user.CreateDMChannelAsync();
            // TODO: save welcome pack in database to user post command in game
            // TODO: check if user is registered
            await dmChannel.SendMessageAsync("🎁 Você recebeu seu Welcome Pack! Seja bem-vindo ao servidor!");

            await component.RespondAsync("📩 Seu Welcome Pack foi enviado na sua DM!", ephemeral: true);
        }
        catch (Exception)
        {
            await component.RespondAsync(
                "⚠ Não consegui enviar sua DM. Certifique-se de que suas mensagens diretas estão ativadas!",
                ephemeral: true
            );
        }
    }
}