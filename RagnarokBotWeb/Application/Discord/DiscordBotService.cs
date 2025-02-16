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

        // Configure o cliente com os intents necessários
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds |
                             GatewayIntents.GuildMessages |
                             GatewayIntents.MessageContent
        };
        _client = new DiscordSocketClient(config);

        // Obtenha o token de alguma fonte, por exemplo, variáveis de ambiente ou IConfiguration
        // _token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
        _token = "MTM0MDE3NjI0MTA5NzE4MzM0Mw.Gj_E-C.cP30r5RLRLnXwE6bAzWBfCrN2dVXn52J21MHoY";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Registre os eventos do Discord
        _client.Log += LogAsync;
        _client.MessageReceived += MessageReceivedAsync;

        if (string.IsNullOrEmpty(_token))
        {
            _logger.LogError("O token do Discord não está definido!");
            return;
        }

        // Login e início do bot
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();

        _logger.LogInformation("Bot do Discord iniciado.");

        // Mantém o serviço rodando até o término
        try
        {
            await Task.Delay(-1, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Finalização do serviço
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