using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Handlers;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Application.Discord.Events.Interactions;

public class WelcomePackEvent : IInteractionEventHandler
{
    private readonly ILogger<WelcomePackEvent> _logger;
    private readonly IServiceProvider _serviceProvider;

    public WelcomePackEvent(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<WelcomePackEvent>();
    }

    public async Task HandleAsync(SocketInteraction interaction)
    {
        if (interaction is not SocketMessageComponent component) return;

        var user = component.User as SocketGuildUser;

        try
        {
            // TODO: change to custom exception
            if (user is null) throw new Exception($"User {component.User} not found");

            // TODO: check if guild id is null and throws an exception
            var guildDiscordId = interaction.GuildId ?? 0L;
            var userDiscordId = user.Id;

            using var scope = _serviceProvider.CreateScope();
            var guildService = scope.ServiceProvider.GetRequiredService<IGuildService>();
            var playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();

            var guild = await guildService.FindByDiscordIdAsync(guildDiscordId);
            var player = await playerService.FindByGuildIdAndDiscordIdAsync(guild.Id, userDiscordId);

            var dmChannel = await user.CreateDMChannelAsync();

            if (player != null && AlreadyReceived(player))
            {
                var dmWarningMessage = GetDmWarningMessage(player);
                await dmChannel.SendMessageAsync(dmWarningMessage);
                await component.RespondAsync($"{DiscordEmoji.EnvelopeWithArrow} Você já solicitou seu Welcome Pack!",
                    ephemeral: true);
                return;
            }

            var registerId = Guid.NewGuid();

            if (player is not null)
            {
                player.RegisterId = registerId;
                player.Status = EPlayerStatus.Registering;
                await playerService.UpdatePlayerAsync(player);
            }
            else
            {
                var newPlayer = new Player
                {
                    DiscordId = userDiscordId,
                    RegisterId = registerId,
                    ScumServer = guild.ScumServer,
                    Status = EPlayerStatus.Registering
                };
                await playerService.AddPlayerAsync(newPlayer);
            }

            await dmChannel.SendMessageAsync(
                $"{DiscordEmoji.Gift} Para receber seu Welcome Pack copie e cole o código {BuildWelcomePackCommand(registerId)} no chat do jogo!");

            await component.RespondAsync($"{DiscordEmoji.EnvelopeWithArrow} Seu Welcome Pack foi enviado na sua DM!",
                ephemeral: true);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while handling Welcome Pack event.");
            await component.RespondAsync(
                $"{DiscordEmoji.Warning} Não consegui enviar sua DM. Certifique-se de que suas mensagens diretas estão ativadas!",
                ephemeral: true
            );
        }
    }

    private static bool AlreadyReceived(Player player)
    {
        return player.Status is not (EPlayerStatus.Unregistered or null);
    }

    private static string GetDmWarningMessage(Player player)
    {
        var message =
            $"Vi que você pediu um novo Welcome Pack, infelizmente nao posso te ajudar {DiscordEmoji.Pensive}.";
        if (player.RegisterId is not null)
            message += $" Caso precise, o primeiro código gerado foi {BuildWelcomePackCommand(player.RegisterId)}";

        return message;
    }

    private static string BuildWelcomePackCommand(Guid? registerId)
    {
        return "!WelcomePack_" + registerId;
    }
}