using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Handlers;
using RagnarokBotWeb.Crosscutting.Utils;
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
            var playerRegisterService = scope.ServiceProvider.GetRequiredService<IPlayerRegisterService>();

            var guild = await guildService.FindByDiscordIdAsync(guildDiscordId);
            var playerRegister = await playerRegisterService.FindByGuildIdAndDiscordIdAsync(guild.Id, userDiscordId);

            var dmChannel = await user.CreateDMChannelAsync();

            if (playerRegister != null)
            {
                var dmWarningMessage = GetDmWarningMessage(playerRegister);
                await dmChannel.SendMessageAsync(dmWarningMessage);
                await component.RespondAsync($"{DiscordEmoji.EnvelopeWithArrow} Você já solicitou seu Welcome Pack!",
                    ephemeral: true);
                return;
            }

            var registerId = StringUtils.RandomNumericString(20)!;

            var newPlayerRegister = new PlayerRegister
            {
                WelcomePackId = registerId,
                DiscordId = userDiscordId,
                ScumServer = guild.ScumServer,
                Status = EPlayerRegisterStatus.Registering
            };
            await playerRegisterService.SaveAsync(newPlayerRegister);

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

    private static string GetDmWarningMessage(PlayerRegister playerRegister)
    {
        var status = playerRegister.Status;
        return status switch
        {
            EPlayerRegisterStatus.Registering =>
                "Vi que você já pediu o Welcome Pack e ainda não colou o código no chat do jogo. " +
                $"Copie e cole o código {BuildWelcomePackCommand(playerRegister.WelcomePackId)} no chat do jogo!",
            EPlayerRegisterStatus.Registered =>
                $"Vi que você pediu um novo Welcome Pack, infelizmente não posso te ajudar {DiscordEmoji.Pensive}.",
            _ => throw new ArgumentOutOfRangeException(nameof(status), $"Unexpected player register status: {status}.")
        };
    }

    private static string BuildWelcomePackCommand(string registerId)
    {
        return "!welcomepack" + registerId;
    }
}