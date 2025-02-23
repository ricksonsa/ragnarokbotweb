using Discord;
using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Handlers;

namespace RagnarokBotWeb.Application.Discord;

public class DiscordBotService : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<DiscordBotService> _logger;
    private readonly IMessageEventHandlerFactory _messageEventHandlerFactory;
    private readonly string _token;

    public DiscordBotService(ILogger<DiscordBotService> logger, IMessageEventHandlerFactory messageEventHandlerFactory)
    {
        _logger = logger;
        _messageEventHandlerFactory = messageEventHandlerFactory;

        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds |
                             GatewayIntents.GuildMessages |
                             GatewayIntents.MessageContent
        };
        _client = new DiscordSocketClient(config);

        // _token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
        _token = "MTM0MDE3NjI0MTA5NzE4MzM0Mw.Gj_E-C.cP30r5RLRLnXwE6bAzWBfCrN2dVXn52J21MHoY";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.Log += LogAsync;
        _client.MessageReceived += MessageReceivedAsync;
        _client.InteractionCreated += InteractionCreated;

        if (string.IsNullOrEmpty(_token))
        {
            _logger.LogError("Discord token is not set!");
            return;
        }

        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();

        _logger.LogInformation("Discord bot started.");

        try
        {
            await Task.Delay(-1, stoppingToken);
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task InteractionCreated(SocketInteraction interaction)
    {
        _logger.LogInformation("Interaction comming from discord.");

        if (interaction is SocketMessageComponent component)
        {
            if (component.Data.CustomId == "welcome_pack")
            {
                var user = component.User as SocketGuildUser;

                try
                {
                    // Tenta enviar a DM
                    var dmChannel = await user.CreateDMChannelAsync();
                    await dmChannel.SendMessageAsync("🎁 Você recebeu seu Welcome Pack! Seja bem-vindo ao servidor!");

                    // Responde no canal para confirmar o envio
                    await component.RespondAsync("📩 Seu Welcome Pack foi enviado na sua DM!", ephemeral: true);
                }
                catch (Exception)
                {
                    await component.RespondAsync("⚠️ Não consegui enviar sua DM. Certifique-se de que suas mensagens diretas estão ativadas!", ephemeral: true);
                }
            }
        }
    }

    private Task LogAsync(LogMessage log)
    {
        _logger.LogInformation(log.ToString());
        return Task.CompletedTask;
    }

    private Task MessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.IsBot)
            return Task.CompletedTask;

        // Isso aqui para pegar o tenant do usuário
        if (message.Channel is SocketTextChannel textChannel)
        {
            ulong guildId = textChannel.Guild.Id;
            Console.WriteLine($"Guild ID: {guildId}");
        }
        else
        {
            Console.WriteLine("Mensagem recebida em DM.");
        }

        var handler = _messageEventHandlerFactory.GetHandler(message);
        if (handler != null)
            _ = handler.HandleAsync(message);
        else
            _ = message.Channel.SendMessageAsync($"Event with name = {message.Content} not found.");

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.LogoutAsync();
        await _client.StopAsync();
        await base.StopAsync(cancellationToken);
    }
}