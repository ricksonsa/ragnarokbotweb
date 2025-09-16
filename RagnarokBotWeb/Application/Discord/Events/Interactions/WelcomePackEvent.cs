using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Handlers;
using RagnarokBotWeb.Application.Models;
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
            var discordService = scope.ServiceProvider.GetRequiredService<IDiscordService>();

            var guild = await guildService.FindByDiscordIdAsync(guildDiscordId);
            var playerRegister = await playerRegisterService.FindByGuildIdAndDiscordIdAsync(guild.Id, userDiscordId);

            var dmChannel = await user.CreateDMChannelAsync();

            if (playerRegister != null)
            {
                try
                {
                    var dmWarningMessage = GetDmWarningMessage(playerRegister);
                    await dmChannel.SendMessageAsync(dmWarningMessage);
                    await component.RespondAsync($"{DiscordEmoji.EnvelopeWithArrow} You have already requested your Welcome Pack!",
                        ephemeral: true);
                }
                catch { }

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
            var code = BuildWelcomePackCommand(registerId);

            var embed = new CreateEmbed
            {
                Title = "THE SCUMBOT REGISTRATION",
                Text = $"{DiscordEmoji.Gift} To receive your Welcome Pack, copy and paste the code bellow into the game chat!\n{code}",
            };

            embed.AddField(new CreateEmbedField("Server", guild.ScumServer.Name!));
            embed.AddField(new CreateEmbedField("Welcome Pack Code", code));

            await discordService.SendEmbedToDmChannel(embed, dmChannel);
            await component.RespondAsync($"{DiscordEmoji.EnvelopeWithArrow} Your Welcome Pack has been sent to your DM!", ephemeral: true);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while handling Welcome Pack event.");
            await component.RespondAsync(
                $"{DiscordEmoji.Warning} I couldn't send your DM. Make sure your direct messages are turned on!",
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
                "I saw that you already requested the Welcome Pack and haven't yet pasted the code into the game chat. " +
                $"Copy and paste the code bellow into the game chat!\n{BuildWelcomePackCommand(playerRegister.WelcomePackId)}",
            EPlayerRegisterStatus.Registered =>
                $"I saw that you requested a new Welcome Pack, unfortunately I can't help you {DiscordEmoji.Pensive}.",
            _ => throw new ArgumentOutOfRangeException(nameof(status), $"Unexpected player register status: {status}.")
        };
    }

    private static string BuildWelcomePackCommand(string registerId)
    {
        return "!welcomepack" + registerId;
    }
}