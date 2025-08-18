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

        private async Task PingLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(8), token);

                try
                {
                    var now = DateTime.UtcNow;

                    foreach (var serverPair in _bots)
                    {
                        foreach (var botPair in serverPair.Value)
                        {
                            var bot = botPair.Value;
                            if (!botPair.Value.LastPinged.HasValue || (now - bot.LastPinged!.Value).TotalMinutes >= 8)
                            {
                                await SendCommandAsync(serverPair.Key, bot.Guid.ToString(), new BotCommand().Reconnect());
                                DisconnectBot(bot);
                            }
                        }
                    }
                }
                catch (Exception)
                { }
            }
        }

        private void DisconnectBot(BotUser bot)
        {
            if (_bots.TryGetValue(bot.ServerId, out var botsForServer))
            {
                botsForServer.TryRemove(bot.Guid.ToString(), out _);
                _logger.LogInformation("Disconnecting Bot {Bot}", bot);
            }

            try
            {
                bot.TcpClient?.Dispose();
            }
            catch (Exception)
            { }
        }

        public async Task SendCommandAsync(long serverId, BotCommand command)
        {
            if (!_bots.TryGetValue(serverId, out var botsForServer)) return;

            var bot = botsForServer.Values
                .Where(b => b.TcpClient?.Connected == true)
                .OrderBy(b => b.LastCommand ?? DateTime.MinValue)
                .FirstOrDefault();

            if (bot != null)
            {
                try
                {
                    await SendLengthedMessage(command, bot);
                    bot.LastCommand = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send command to bot {Bot}", bot);
                    DisconnectBot(bot);
                }
            }
            else
            {
                _logger.LogWarning("No connected bots found for server {ServerId}", serverId);
            }
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

                _logger.LogInformation("Bot {Guid} connected", guid);

                // --- Message loop ---
                while (!token.IsCancellationRequested && client.Connected)
                {
                    var line = await ReceiveStringAsync(stream, token);
                    if (string.IsNullOrEmpty(line)) break;

                    if (line.StartsWith("ACK:", StringComparison.OrdinalIgnoreCase))
                    {
                        var ackParts = line.Split(':', 3);
                        if (ackParts.Length == 3 &&
                            long.TryParse(ackParts[1], out long ackServerId) &&
                            Guid.TryParse(ackParts[2], out var ackGuid))
                        {
                            var serverBotsAck = _bots.GetOrAdd(
                                ackServerId,
                                _ => new ConcurrentDictionary<string, BotUser>());

                            serverBotsAck.AddOrUpdate(
                                ackGuid.ToString(),
                                _ =>
                                {
                                    if (bot is not null) DisconnectBot(bot);
                                    var newBot = new BotUser(ackGuid)
                                    {
                                        TcpClient = client,
                                        ServerId = ackServerId,
                                        LastInteracted = DateTime.UtcNow
                                    };
                                    _logger.LogInformation("Bot {Guid} connected", ackGuid);
                                    return newBot;
                                },
                                (_, existingBot) =>
                                {
                                    existingBot.LastInteracted = DateTime.UtcNow;
                                    _logger.LogDebug("Bot {Guid} ACK updated", ackGuid);
                                    return existingBot;
                                });
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Unknown message from bot: {Msg}", line);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Client error");
            }
            finally
            {
                if (bot != null) DisconnectBot(bot);
                client.Dispose();
            }
        }

        private async Task<string?> ReceiveStringAsync(NetworkStream stream, CancellationToken token)
        {
            var lengthBuffer = new byte[4];
            int read = await stream.ReadAsync(lengthBuffer, 0, 4, token);
            if (read == 0) return null;

            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBuffer);

            int length = BitConverter.ToInt32(lengthBuffer, 0);
            if (length <= 0) return null;

            var buffer = new byte[length];
            int offset = 0;
            while (offset < length)
            {
                int chunk = await stream.ReadAsync(buffer, offset, length - offset, token);
                if (chunk == 0) return null; // disconnected
                offset += chunk;
            }

            return Encoding.UTF8.GetString(buffer);
        }


        public async Task StartAsync(CancellationToken token = default)
        {
            _ = Task.Run(() => PingLoop(token), token);

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

