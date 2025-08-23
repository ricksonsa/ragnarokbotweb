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
        private readonly object _connectionLock = new object();

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

                        // Check if TCP connection is actually alive
                        bool isTcpAlive = IsTcpConnectionAlive(bot);

                        // If TCP is disconnected and no recent ping, disconnect bot
                        if (!isTcpAlive)
                        {
                            if (!bot.LastPinged.HasValue || (now - bot.LastPinged.Value).TotalMinutes >= 10)
                            {
                                _logger.LogInformation("Bot {Guid} TCP disconnected and no recent game ping - disconnecting", bot.Guid);
                                DisconnectBot(bot);
                                continue;
                            }
                        }

                        // Bot is TCP connected - check if it needs a game reconnect
                        if (isTcpAlive)
                        {
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
        }

        private bool IsTcpConnectionAlive(BotUser bot)
        {
            if (bot.TcpClient?.Connected != true) return false;

            try
            {
                // Use Socket.Poll to check if connection is still alive
                var socket = bot.TcpClient.Client;
                return !(socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch
            {
                return false;
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
                bot.TcpClient?.Close();
                bot.TcpClient?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error disposing bot TCP client {Guid}", bot.Guid);
            }
        }

        private static async Task SendLengthedMessage(BotCommand command, BotUser bot)
        {
            if (bot.TcpClient?.Connected != true)
                throw new InvalidOperationException("Bot TCP client is not connected");

            var body = MessagePackSerializer.Serialize(command);
            var lengthPrefix = BitConverter.GetBytes(body.Length);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthPrefix);

            var stream = bot.TcpClient.GetStream();

            // Add timeouts for network operations
            stream.WriteTimeout = 30000; // 30 seconds

            await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
            await stream.WriteAsync(body, 0, body.Length);
            await stream.FlushAsync();
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            BotUser? bot = null;
            NetworkStream? stream = null;

            try
            {
                // Configure client socket settings
                client.ReceiveTimeout = 60000; // 1 minute
                client.SendTimeout = 30000;    // 30 seconds
                client.NoDelay = true;         // Disable Nagle's algorithm

                // Configure socket-level keepalive
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 30);
                client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 10);
                client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);

                stream = client.GetStream();
                stream.ReadTimeout = 60000; // 1 minute read timeout

                // --- Handshake ---
                var handshake = await ReceiveStringAsync(stream, token);
                if (string.IsNullOrWhiteSpace(handshake))
                {
                    _logger.LogWarning("Empty handshake received from {RemoteEndpoint}", client.Client.RemoteEndPoint);
                    return;
                }

                var parts = handshake.Split(':', 2);
                if (parts.Length < 2)
                {
                    _logger.LogWarning("Invalid handshake format from {RemoteEndpoint}: {Handshake}", client.Client.RemoteEndPoint, handshake);
                    return;
                }
                if (!long.TryParse(parts[0], out var serverId))
                {
                    _logger.LogWarning("Invalid server ID in handshake from {RemoteEndpoint}: {ServerId}", client.Client.RemoteEndPoint, parts[0]);
                    return;
                }
                if (!Guid.TryParse(parts[1], out var guid))
                {
                    _logger.LogWarning("Invalid GUID in handshake from {RemoteEndpoint}: {Guid}", client.Client.RemoteEndPoint, parts[1]);
                    return;
                }

                lock (_connectionLock)
                {
                    bot = new BotUser(guid)
                    {
                        TcpClient = client,
                        ServerId = serverId,
                        LastInteracted = DateTime.UtcNow
                    };

                    var serverBots = _bots.GetOrAdd(serverId, _ => new ConcurrentDictionary<string, BotUser>());

                    // Remove any existing bot with same GUID (handles reconnects)
                    if (serverBots.TryGetValue(guid.ToString(), out var existingBot))
                    {
                        _logger.LogInformation("Replacing existing bot connection for {Guid}", guid);
                        try
                        {
                            existingBot.TcpClient?.Close();
                            existingBot.TcpClient?.Dispose();
                        }
                        catch { }
                    }

                    serverBots[guid.ToString()] = bot;
                }

                _logger.LogInformation("Bot {Guid} connected to server {ServerId} from {RemoteEndpoint}", guid, serverId, client.Client.RemoteEndPoint);

                // --- Message loop ---
                while (!token.IsCancellationRequested && client.Connected && IsTcpConnectionAlive(bot))
                {
                    try
                    {
                        // Just keep connection alive by reading any incoming data
                        var line = await ReceiveStringAsync(stream, token);
                        if (string.IsNullOrEmpty(line))
                        {
                            _logger.LogDebug("Bot {Guid} connection closed gracefully", guid);
                            break;
                        }

                        // Update last interaction time for any message received
                        bot.LastInteracted = DateTime.UtcNow;

                        // Handle keepalive messages
                        if (line.StartsWith("KEEPALIVE:"))
                        {
                            _logger.LogDebug("Keepalive received from bot {Guid}", guid);
                            continue;
                        }

                        _logger.LogDebug("Received from bot {Guid}: {Message}", guid, line);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (IOException ioEx) when (ioEx.InnerException is SocketException)
                    {
                        _logger.LogDebug("Socket exception for bot {Guid}: {Message}", guid, ioEx.Message);
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error reading from bot {Guid}", guid);
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                _logger.LogDebug("Client handling cancelled for bot {Guid}", bot?.Guid);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Client error for bot {Guid}", bot?.Guid);
            }
            finally
            {
                if (bot != null)
                {
                    DisconnectBot(bot);
                }

                try { stream?.Dispose(); } catch { }
                try { client.Close(); } catch { }
                try { client.Dispose(); } catch { }
            }
        }

        private async Task<string?> ReceiveStringAsync(NetworkStream stream, CancellationToken token)
        {
            try
            {
                var lengthBuffer = new byte[4];
                int totalRead = 0;

                // Read length prefix with proper error handling
                while (totalRead < 4)
                {
                    int read = await stream.ReadAsync(lengthBuffer, totalRead, 4 - totalRead, token);
                    if (read == 0) return null; // Connection closed
                    totalRead += read;
                }

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBuffer);

                int length = BitConverter.ToInt32(lengthBuffer, 0);
                if (length <= 0 || length > 1024 * 1024) // Reasonable size limit
                {
                    _logger.LogWarning("Invalid message length received: {Length}", length);
                    return null;
                }

                var buffer = new byte[length];
                int offset = 0;
                while (offset < length)
                {
                    int chunk = await stream.ReadAsync(buffer, offset, length - offset, token);
                    if (chunk == 0) return null; // Connection closed
                    offset += chunk;
                }

                return Encoding.UTF8.GetString(buffer);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return null;
            }
            catch (IOException ioEx)
            {
                _logger.LogDebug(ioEx, "IO error receiving string message");
                return null;
            }
            catch (SocketException sockEx)
            {
                _logger.LogDebug(sockEx, "Socket error receiving string message");
                return null;
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
                        _ = Task.Run(() => HandleClientAsync(client, token), token);
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

        /// <summary>
        /// Send the command to any available bot for the server. 
        /// Prioritize bots with recent game ping, but fall back to any connected bot. 
        /// Prefer recently pinged bots.
        /// Then by least recently used.
        /// </summary>
        /// <param name="serverId">Server ID</param>
        /// <param name="command">BotCommand</param>
        /// <returns></returns>
        public async Task SendCommandAsync(long serverId, BotCommand command)
        {
            if (!_bots.TryGetValue(serverId, out var botsForServer))
            {
                _logger.LogWarning("No bots registered for server {ServerId}", serverId);
                return;
            }

            var bot = botsForServer.Values
                .Where(b => IsTcpConnectionAlive(b))
                .OrderByDescending(b => b.LastPinged ?? DateTime.MinValue)
                .ThenBy(b => b.LastCommand ?? DateTime.MinValue)
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
                IsTcpConnectionAlive(bot))
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
                IsTcpConnectionAlive(b) ||
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

            var bots = botsForServer.Values.Where(b => IsTcpConnectionAlive(b)).ToList();

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