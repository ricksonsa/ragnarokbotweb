using MessagePack;
using Microsoft.Extensions.Options;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Configuration.Data;
using Shared.Models;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RagnarokBotWeb.Application.BotServer
{
    public class BotSocketServer
    {
        private readonly TcpListener _listener;
        private readonly ILogger<BotSocketServer> _logger;
        private readonly Timer _timer;
        private readonly ConcurrentDictionary<long, ConcurrentDictionary<string, BotUser>> _bots = new();

        public BotSocketServer(ILogger<BotSocketServer> logger, IOptions<AppSettings> options)
        {
            _listener = new TcpListener(IPAddress.Any, options.Value.SocketServerPort);
            _logger = logger;
            _timer = new Timer(TimerCallback, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
        }

        private void TimerCallback(object? state)
        {
            var now = DateTime.UtcNow;

            foreach (var serverPair in _bots)
            {
                foreach (var botPair in serverPair.Value)
                {
                    var bot = botPair.Value;
                    var diff = (now - bot.LastInteracted).TotalMinutes;
                    if (diff >= 5)
                        DisconnectBot(bot);
                }
            }
        }

        private void DisconnectBot(BotUser bot)
        {
            if (_bots.TryGetValue(bot.ServerId, out var botsForServer))
            {
                botsForServer.TryRemove(bot.Guid.ToString(), out _);
            }

            bot.TcpClient?.Dispose();
            _logger.LogInformation("Disconnecting Bot {Bot}", bot);
        }

        private static async Task SendLengthedMessage(BotCommand command, BotUser bot)
        {
            var body = MessagePackSerializer.Serialize(command);
            var lengthPrefix = BitConverter.GetBytes(body.Length);
            await bot.TcpClient!.GetStream().WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
            await bot.TcpClient!.GetStream().WriteAsync(body, 0, body.Length);
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using var stream = client.GetStream();
            var buffer = new byte[1024];

            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
            if (bytesRead == 0) return;

            var data = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
            var parts = data.Split(':');
            if (parts.Length < 2) { client.Dispose(); return; }

            if (!long.TryParse(parts[0], out var serverId)) return;

            var guidString = parts[1];
            if (!Guid.TryParse(guidString, out var guid)) return;

            var bot = new BotUser(guid)
            {
                TcpClient = client,
                ServerId = serverId
            };

            var serverBots = _bots.GetOrAdd(serverId, _ => new ConcurrentDictionary<string, BotUser>());
            serverBots[guidString] = bot;

            _logger.LogInformation("Connecting bot {Bot} server {Server}", bot, serverId);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead == 0) break; // disconnected

                    var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    if (message.Contains("ACK"))
                    {
                        if (serverBots.TryGetValue(guidString, out var existingBot))
                        {
                            existingBot.LastInteracted = DateTime.UtcNow;
                            serverBots[guidString] = existingBot;
                        }
                        _logger.LogDebug("Updating connected bot {Bot} server {Server}", bot, serverId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Client error for bot {Bot}", bot);
                    break;
                }
            }

            DisconnectBot(bot);
        }

        public async Task StartAsync(CancellationToken token = default)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    _listener.Start();
                    _logger.LogInformation("Socket server started on {Port}", ((IPEndPoint)_listener.LocalEndpoint).Port);

                    // Accept loop
                    while (!token.IsCancellationRequested)
                    {
                        TcpClient client;
                        try
                        {
                            client = await _listener.AcceptTcpClientAsync(token);
                        }
                        catch (OperationCanceledException)
                        {
                            break; // shutdown requested
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error accepting client");
                            await Task.Delay(1000, token); // small backoff, then continue
                            continue;
                        }

                        _logger.LogInformation("New client connected.");
                        _ = HandleClientAsync(client, token); // fire-and-forget
                    }
                }
                catch (Exception ex) when (!token.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Listener crashed, will retry in 5 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(5), token); // backoff before retry
                }
                finally
                {
                    try { _listener.Stop(); } catch { }
                }
            }
        }

        public async Task SendCommandAsync(long serverId, BotCommand command)
        {
            if (!_bots.TryGetValue(serverId, out var botsForServer)) return;

            var bot = botsForServer.Values
                .Where(b => b.LastPinged.HasValue)
                .OrderBy(b => b.LastCommand)
                .FirstOrDefault();

            if (bot != null && bot.TcpClient?.Connected == true)
            {
                try
                {
                    await SendLengthedMessage(command, bot);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send command to bot {Bot}", bot);
                    DisconnectBot(bot);
                }
            }
        }


        public async Task SendCommandAsync(long serverId, string guid, BotCommand command)
        {
            if (_bots.TryGetValue(serverId, out var botsForServer) &&
                botsForServer.TryGetValue(guid, out var bot) &&
                bot.TcpClient?.Connected == true)
            {
                try
                {
                    await SendLengthedMessage(command, bot);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send command to bot {Bot}", bot);
                    DisconnectBot(bot);
                }
            }
        }

        public List<BotUser> GetBots(long serverId) =>
       _bots.TryGetValue(serverId, out var botsForServer)
           ? botsForServer.Values.ToList()
           : [];

        public bool IsBotConnected(long serverId) =>
            _bots.TryGetValue(serverId, out var botsForServer) &&
            botsForServer.Values.Any(b => b.LastPinged.HasValue);

        public void BotPingUpdate(long serverId, Guid guid, string steamId)
        {
            if (_bots.TryGetValue(serverId, out var botsForServer) &&
                botsForServer.TryGetValue(guid.ToString(), out var bot))
            {
                bot.SteamId = steamId;
                bot.LastPinged = DateTime.UtcNow;
                botsForServer[guid.ToString()] = bot;
            }
        }

        public async Task SendCommandToAll(long serverId, BotCommand command)
        {
            List<BotUser> bots;

            lock (_bots)
            {
                bots = _bots[serverId].Values.ToList();
            }

            foreach (var bot in bots)
            {
                if (bot.TcpClient != null && bot.TcpClient.Connected)
                    try
                    {
                        await SendLengthedMessage(command, bot);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send to bot {Bot}", bot);
                        DisconnectBot(bot);
                    }
            }
        }
    }

}

