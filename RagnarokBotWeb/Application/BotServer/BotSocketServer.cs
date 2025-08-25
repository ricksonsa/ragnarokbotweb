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
        private readonly ConcurrentDictionary<long, ConcurrentDictionary<string, BotUser>> _bots = new();

        public BotSocketServer(ILogger<BotSocketServer> logger, IOptions<AppSettings> options)
        {
            _listener = new TcpListener(IPAddress.Any, options.Value.SocketServerPort);
            _logger = logger;
        }

        private async Task GameMonitorLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), token); // Check every minute

                var now = DateTime.UtcNow;

                foreach (var serverPair in _bots.ToList())
                {
                    foreach (var botPair in serverPair.Value.ToList())
                    {
                        var bot = botPair.Value;

                        // If TCP is disconnected and no recent ping, disconnect bot
                        if (bot.TcpClient?.Connected != true)
                        {
                            if (!bot.LastPinged.HasValue || (now - bot.LastPinged.Value).TotalMinutes >= 10)
                            {
                                _logger.LogInformation("Bot {Guid} TCP disconnected and no recent game ping - disconnecting", bot.Guid);
                                DisconnectBot(bot);
                            }
                            continue;
                        }

                        // Bot is TCP connected - check if it needs a game reconnect
                        if (bot.LastPinged.HasValue)
                        {
                            var minutesSinceLastPing = (now - bot.LastPinged.Value).TotalMinutes;

                            // If bot hasn't pinged in game for 3+ minutes, send reconnect command
                            if (minutesSinceLastPing >= 3)
                            {
                                // Don't spam reconnect commands - only send if we haven't sent one recently
                                if (!bot.LastReconnectSent.HasValue || (now - bot.LastReconnectSent.Value).TotalMinutes >= 2)
                                {
                                    _logger.LogInformation("Bot {Guid} hasn't pinged in {Minutes} minutes - sending reconnect command",
                                        bot.Guid, Math.Round(minutesSinceLastPing, 1));

                                    try
                                    {
                                        await SendLengthedMessage(new BotCommand().Reconnect(), bot);
                                        bot.LastReconnectSent = now;
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Failed to send reconnect to bot {Guid}", bot.Guid);
                                        DisconnectBot(bot);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Bot connected but never pinged - send initial reconnect after 2 minutes
                            var minutesSinceConnection = (now - bot.LastInteracted).TotalMinutes;
                            if (minutesSinceConnection >= 2)
                            {
                                if (!bot.LastReconnectSent.HasValue || (now - bot.LastReconnectSent.Value).TotalMinutes >= 2)
                                {
                                    _logger.LogInformation("Bot {Guid} connected but never pinged - sending initial reconnect", bot.Guid);

                                    try
                                    {
                                        await SendLengthedMessage(new BotCommand().Reconnect(), bot);
                                        bot.LastReconnectSent = now;
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Failed to send initial reconnect to bot {Guid}", bot.Guid);
                                        DisconnectBot(bot);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DisconnectBot(BotUser bot)
        {
            if (_bots.TryGetValue(bot.ServerId, out var botsForServer))
            {
                botsForServer.TryRemove(bot.Guid.ToString(), out _);
                _logger.LogInformation("Disconnecting Bot {Guid} from server {ServerId}", bot.Guid, bot.ServerId);
            }

            try
            {
                bot.TcpClient?.Dispose();
            }
            catch (Exception)
            { }
        }

        private static async Task SendLengthedMessage(BotCommand command, BotUser bot)
        {
            var body = MessagePackSerializer.Serialize(command);
            var lengthPrefix = BitConverter.GetBytes(body.Length);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthPrefix);

            var stream = bot.TcpClient!.GetStream();
            await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
            await stream.WriteAsync(body, 0, body.Length);
            await stream.FlushAsync();
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using var stream = client.GetStream();
            BotUser? bot = null;

            try
            {
                // --- Handshake ---
                var handshake = await ReceiveStringAsync(stream, token);
                if (string.IsNullOrWhiteSpace(handshake)) return;

                var parts = handshake.Split(':', 2);
                if (parts.Length < 2) return;
                if (!long.TryParse(parts[0], out var serverId)) return;
                if (!Guid.TryParse(parts[1], out var guid)) return;

                bot = new BotUser(guid)
                {
                    TcpClient = client,
                    ServerId = serverId,
                    LastInteracted = DateTime.UtcNow
                };

                var serverBots = _bots.GetOrAdd(serverId, _ => new ConcurrentDictionary<string, BotUser>());
                serverBots[guid.ToString()] = bot;

                _logger.LogInformation("Bot {Guid} connected to server {ServerId}", guid, serverId);

                // --- Simplified Message loop - no ACK required ---
                while (!token.IsCancellationRequested && client.Connected)
                {
                    // Just keep connection alive by reading any incoming data
                    var line = await ReceiveStringAsync(stream, token);
                    if (string.IsNullOrEmpty(line))
                    {
                        _logger.LogDebug("Bot {Guid} connection closed", guid);
                        break;
                    }

                    // Update last interaction time for any message received
                    bot.LastInteracted = DateTime.UtcNow;

                    // Log any messages for debugging but don't require specific format
                    _logger.LogDebug("Received from bot {Guid}: {Message}", guid, line);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Client error for bot {Guid}", bot?.Guid);
            }
            finally
            {
                if (bot != null) DisconnectBot(bot);
                client.Dispose();
            }
        }

        private async Task<string?> ReceiveStringAsync(NetworkStream stream, CancellationToken token)
        {
            try
            {
                var lengthBuffer = new byte[4];
                int read = await stream.ReadAsync(lengthBuffer, 0, 4, token);
                if (read == 0) return null;

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBuffer);

                int length = BitConverter.ToInt32(lengthBuffer, 0);
                if (length <= 0 || length > 1024 * 1024) // Reasonable size limit
                    return null;

                var buffer = new byte[length];
                int offset = 0;
                while (offset < length)
                {
                    int chunk = await stream.ReadAsync(buffer, offset, length - offset, token);
                    if (chunk == 0) return null;
                    offset += chunk;
                }

                return Encoding.UTF8.GetString(buffer);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error receiving string message");
                return null;
            }
        }

        public async Task StartAsync(CancellationToken token = default)
        {
            _ = Task.Run(() => GameMonitorLoop(token), token);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    _listener.Start();
                    _logger.LogInformation("Socket server started on {Port}", ((IPEndPoint)_listener.LocalEndpoint).Port);

                    while (!token.IsCancellationRequested)
                    {
                        TcpClient client;
                        try
                        {
                            client = await _listener.AcceptTcpClientAsync(token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error accepting client");
                            await Task.Delay(1000, token);
                            continue;
                        }

                        _logger.LogInformation("New client connected from {RemoteEndpoint}", client.Client.RemoteEndPoint);
                        _ = HandleClientAsync(client, token);
                    }
                }
                catch (Exception ex) when (!token.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Listener crashed, will retry in 5 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                }
                finally
                {
                    try { _listener.Stop(); } catch { }
                }
            }
        }

        // Send command to any available bot for the server
        public async Task SendCommandAsync(long serverId, BotCommand command)
        {
            if (!_bots.TryGetValue(serverId, out var botsForServer))
            {
                _logger.LogWarning("No bots registered for server {ServerId}", serverId);
                return;
            }

            // Prioritize bots with recent game ping, but fall back to any connected bot
            var bot = botsForServer.Values
                .Where(b => b.TcpClient?.Connected == true)
                .OrderByDescending(b => b.LastPinged ?? DateTime.MinValue) // Prefer recently pinged bots
                .ThenBy(b => b.LastCommand ?? DateTime.MinValue) // Then by least recently used
                .FirstOrDefault();

            if (bot != null)
            {
                try
                {
                    await SendLengthedMessage(command, bot);
                    bot.LastCommand = DateTime.UtcNow;
                    _logger.LogDebug("Command sent to bot {Guid} on server {ServerId}", bot.Guid, serverId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send command to bot {Guid}", bot.Guid);
                    DisconnectBot(bot);
                }
            }
            else
            {
                _logger.LogWarning("No connected bots available for server {ServerId}", serverId);
            }
        }

        // Send command to specific bot
        public async Task SendCommandAsync(long serverId, string guid, BotCommand command)
        {
            if (_bots.TryGetValue(serverId, out var botsForServer) &&
                botsForServer.TryGetValue(guid, out var bot) &&
                bot.TcpClient?.Connected == true)
            {
                try
                {
                    await SendLengthedMessage(command, bot);
                    bot.LastCommand = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send command to bot {Guid}", bot.Guid);
                    DisconnectBot(bot);
                }
            }
            else
            {
                _logger.LogWarning("Bot {Guid} not found or not connected on server {ServerId}", guid, serverId);
            }
        }

        public List<BotUser> GetBots(long serverId) =>
            _bots.TryGetValue(serverId, out var botsForServer)
                ? botsForServer.Values.ToList()
                : new List<BotUser>();

        // Updated to be more flexible - consider bot connected if TCP is up OR recent game ping
        public bool IsBotConnected(long serverId) =>
            _bots.TryGetValue(serverId, out var botsForServer) &&
            botsForServer.Values.Any(b =>
                b.TcpClient?.Connected == true ||
                (b.LastPinged.HasValue && (DateTime.UtcNow - b.LastPinged.Value).TotalMinutes < 5));

        // This is called by your FTP service when it finds a bot GUID in chat logs
        public void BotPingUpdate(long serverId, Guid guid, string steamId)
        {
            if (_bots.TryGetValue(serverId, out var botsForServer) &&
                botsForServer.TryGetValue(guid.ToString(), out var bot))
            {
                bot.SteamId = steamId;
                bot.LastPinged = DateTime.UtcNow;
                _logger.LogDebug("Bot {Guid} ping updated via game chat on server {ServerId}", guid, serverId);
            }
            else
            {
                _logger.LogDebug("Received ping for unknown bot {Guid} on server {ServerId}", guid, serverId);
            }
        }

        public async Task SendCommandToAll(long serverId, BotCommand command)
        {
            if (!_bots.TryGetValue(serverId, out var botsForServer))
            {
                _logger.LogWarning("No bots found for server {ServerId}", serverId);
                return;
            }

            var bots = botsForServer.Values.Where(b => b.TcpClient?.Connected == true).ToList();

            foreach (var bot in bots)
            {
                try
                {
                    await SendLengthedMessage(command, bot);
                    bot.LastCommand = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send command to bot {Guid}", bot.Guid);
                    DisconnectBot(bot);
                }
            }

            _logger.LogInformation("Command sent to {Count} bots on server {ServerId}", bots.Count, serverId);
        }
    }
}