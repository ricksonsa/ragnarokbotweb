using Discord;
using Discord.WebSocket;

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

    private Task LogAsync(LogMessage log)
    {
        _logger.LogInformation(log.ToString());
        return Task.CompletedTask;
    }

    private async Task MessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.IsBot)
            return;

        var handler = _messageEventHandlerFactory.GetHandler(message);
        if (handler != null)
            await handler.HandleAsync(message);
        else
            await message.Channel.SendMessageAsync($"Event with name = {message.Content} not found.");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.LogoutAsync();
        await _client.StopAsync();
        await base.StopAsync(cancellationToken);
    }
}