using Discord;
using Discord.WebSocket;

namespace RagnarokBotWeb.Application.Discord;

public class DiscordBotService : BackgroundService
{
    public static DiscordBotService Instance;
    private readonly DiscordSocketClient _client;

    private readonly ILogger<DiscordBotService> _logger;

    // private readonly string? _token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
    private readonly string? _token = "MTM0MDE3NjI0MTA5NzE4MzM0Mw.Gc4T14.hCHQkbzmeJRToivaS4vs8HA8EbS_yXAc53b4iE";


    public DiscordBotService(ILogger<DiscordBotService> logger, DiscordSocketClient client)
    {
        _logger = logger;
        _client = client;

        Instance = this;
    }

    public bool IsReady { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.Log += LogAsync;

        if (string.IsNullOrEmpty(_token))
        {
            _logger.LogError("Discord token is not set!");
            return;
        }

        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();

        _logger.LogInformation("Discord bot started.");

        _client.Ready += () =>
        {
            _logger.LogInformation("Discord bot ready.");
            _logger.LogInformation("Discord bot has the following Guilds:");
            foreach (var guild in _client.Guilds)
                _logger.LogInformation(
                    $"Guild Id: {guild.Id}, Guild Name: {guild.Name}, Guild Created: {guild.CreatedAt}");
            IsReady = true;
            return Task.CompletedTask;
        };

        try
        {
            await Task.Delay(-1, stoppingToken);
        }
        catch (TaskCanceledException)
        {
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.LogoutAsync();
        await _client.StopAsync();
        await base.StopAsync(cancellationToken);
    }

    private Task LogAsync(LogMessage log)
    {
        _logger.LogInformation(log.ToString());
        return Task.CompletedTask;
    }
}