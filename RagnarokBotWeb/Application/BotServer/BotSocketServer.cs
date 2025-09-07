using MessagePack;
using Microsoft.Extensions.Options;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Configuration.Data;
using Shared.Models;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace RagnarokBotWeb.Application.BotServer
{
    public class BotSocketServer
    {
        private readonly TcpListener _listener;
        private readonly ILogger<BotSocketServer> _logger;
        private readonly ConcurrentDictionary<long, ConcurrentDictionary<string, BotUser>> _bots = new();
        private readonly string _stateFilePath;
        private readonly object _stateFileLock = new object();
        private readonly System.Timers.Timer _stateSaveTimer;
        private readonly System.Timers.Timer _cleanupTimer;

        // Simplified timeouts
        public static readonly TimeSpan PING_TIMEOUT = TimeSpan.FromMinutes(10); // Bot must ping within 5 minutes
        private static readonly TimeSpan CLEANUP_INTERVAL = TimeSpan.FromMinutes(1); // Check every minute
        private static readonly TimeSpan CONNECTION_GRACE_PERIOD = TimeSpan.FromMinutes(5); // Grace period for disconnected bots

        public BotSocketServer(ILogger<BotSocketServer> logger, IOptions<AppSettings> options)
        {
            _listener = new TcpListener(IPAddress.Any, options.Value.SocketServerPort);
            _logger = logger;

            _stateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "bot_state.json");

            // Auto-save state every 5 minutes
            _stateSaveTimer = new System.Timers.Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
            _stateSaveTimer.Elapsed += (s, e) => SaveBotState();
            _stateSaveTimer.AutoReset = true;
            _stateSaveTimer.Enabled = true;

            // Simplified cleanup timer
            _cleanupTimer = new System.Timers.Timer(CLEANUP_INTERVAL.TotalMilliseconds);
            _cleanupTimer.Elapsed += (s, e) => CleanupStaleConnections();
            _cleanupTimer.AutoReset = true;
            _cleanupTimer.Enabled = true;

            LoadBotState();
        }

        #region State Persistence (unchanged for brevity)
        private void SaveBotState()
        {
            try
            {
                lock (_stateFileLock)
                {
                    var stateData = new BotServerState
                    {
                        SavedAt = DateTime.UtcNow,
                        Servers = _bots.ToDictionary(
                            serverPair => serverPair.Key,
                            serverPair => serverPair.Value.Values.Select(bot => new PersistedBotUser
                            {
                                Guid = bot.Guid,
                                ServerId = bot.ServerId,
                                SteamId = bot.SteamId,
                                LastPinged = bot.LastPinged,
                                LastInteracted = bot.LastInteracted,
                                LastCommand = bot.LastCommand,
                                LastReconnectSent = bot.LastReconnectSent,
                            }).ToList()
                        )
                    };

                    var jsonOptions = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var json = JsonSerializer.Serialize(stateData, jsonOptions);
                    var tempFile = _stateFilePath + ".tmp";
                    File.WriteAllText(tempFile, json);

                    if (File.Exists(_stateFilePath))
                        File.Delete(_stateFilePath);

                    File.Move(tempFile, _stateFilePath);

                    var totalBots = stateData.Servers.Sum(s => s.Value.Count);
                    _logger.LogDebug("Bot state saved - {TotalBots} bots across {ServerCount} servers",
                        totalBots, stateData.Servers.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save bot state");
            }
        }

        private void LoadBotState()
        {
            try
            {
                lock (_stateFileLock)
                {
                    if (!File.Exists(_stateFilePath))
                    {
                        _logger.LogInformation("No bot state file found - starting with empty state");
                        return;
                    }

                    var json = File.ReadAllText(_stateFilePath);
                    var stateData = JsonSerializer.Deserialize<BotServerState>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (stateData?.Servers == null) return;

                    var totalBotsLoaded = 0;
                    var now = DateTime.UtcNow;

                    foreach (var serverPair in stateData.Servers)
                    {
                        var serverId = serverPair.Key;
                        var serverBots = _bots.GetOrAdd(serverId, _ => new ConcurrentDictionary<string, BotUser>());

                        foreach (var persistedBot in serverPair.Value)
                        {
                            // Only restore recent bots
                            if ((now - persistedBot.LastInteracted).TotalHours <= 2)
                            {
                                var restoredBot = new BotUser(persistedBot.Guid)
                                {
                                    ServerId = persistedBot.ServerId,
                                    SteamId = persistedBot.SteamId,
                                    LastPinged = persistedBot.LastPinged,
                                    LastInteracted = persistedBot.LastInteracted,
                                    LastCommand = persistedBot.LastCommand,
                                    LastReconnectSent = persistedBot.LastReconnectSent,
                                    TcpClient = null
                                };

                                serverBots[persistedBot.Guid.ToString()] = restoredBot;
                                totalBotsLoaded++;
                            }
                        }
                    }

                    _logger.LogInformation("Loaded {TotalBots} bots from state file", totalBotsLoaded);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load bot state - starting with empty state");
            }
        }

        public void SaveBotStateOnShutdown()
        {
            try
            {
                _stateSaveTimer?.Stop();
                _cleanupTimer?.Stop();
                SaveBotState();
                _logger.LogInformation("Bot state saved on shutdown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving bot state on shutdown");
            }
        }
        #endregion

        // Simplified cleanup - single method handles all cleanup logic
        private void CleanupStaleConnections()
        {
            var now = DateTime.UtcNow;
            var staleBotsRemoved = 0;

            foreach (var serverPair in _bots.ToList())
            {
                var serverId = serverPair.Key;
                var serverBots = serverPair.Value;

                foreach (var botPair in serverBots.ToList())
                {
                    var bot = botPair.Value;
                    var isConnected = bot.TcpClient?.Connected == true;
                    var timeSinceLastPing = bot.LastPinged.HasValue ? now - bot.LastPinged.Value : TimeSpan.MaxValue;
                    var timeSinceLastInteraction = now - bot.LastInteracted;

                    // Simple cleanup logic:
                    // 1. If TCP disconnected AND no ping for grace period -> remove
                    // 2. If TCP connected but no ping for timeout -> send single reconnect
                    if (!isConnected)
                    {
                        if (timeSinceLastInteraction > CONNECTION_GRACE_PERIOD)
                        {
                            _logger.LogInformation("Removing stale bot {Guid} from server {ServerId} (offline {Minutes}min)",
                                bot.Guid, serverId, timeSinceLastInteraction.TotalMinutes);

                            RemoveBotFromCollection(serverId, bot.Guid.ToString());
                            staleBotsRemoved++;
                        }
                    }
                    else if (timeSinceLastPing > PING_TIMEOUT)
                    {
                        // Send reconnect only if we haven't sent one recently
                        if (!bot.LastReconnectSent.HasValue || (now - bot.LastReconnectSent.Value) > PING_TIMEOUT)
                        {
                            _logger.LogInformation("Bot {Guid} needs reconnect (no ping for {Minutes}min)",
                                bot.Guid, timeSinceLastPing.TotalMinutes);

                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await SendLengthedMessage(new BotCommand().Reconnect(), bot);
                                    bot.LastReconnectSent = now;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to send reconnect to bot {Guid}", bot.Guid);
                                    try { bot.TcpClient?.Close(); } catch { }
                                }
                            });
                        }
                    }
                }
            }

            if (staleBotsRemoved > 0)
            {
                _logger.LogDebug("Cleanup completed - removed {Count} stale bots", staleBotsRemoved);
            }
        }

        private void RemoveBotFromCollection(long serverId, string botGuid)
        {
            if (_bots.TryGetValue(serverId, out var botsForServer))
            {
                if (botsForServer.TryRemove(botGuid, out var removedBot))
                {
                    try { removedBot.TcpClient?.Dispose(); } catch { }
                    _ = Task.Run(() => SaveBotState());
                }
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

        // Fix for BotSocketServer - HandleClientAsync method
        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using var stream = client.GetStream();
            BotUser? bot = null;
            var remoteEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";

            try
            {
                // Simplified timeouts
                stream.ReadTimeout = 60000; // 1 minute
                stream.WriteTimeout = 30000; // 30 seconds

                var handshake = await ReceiveStringAsync(stream, token);
                if (string.IsNullOrWhiteSpace(handshake))
                {
                    _logger.LogWarning("Empty handshake from {RemoteEndpoint}", remoteEndpoint);
                    return;
                }

                var parts = handshake.Split(':', 2);
                if (parts.Length < 2 ||
                    !long.TryParse(parts[0], out var serverId) ||
                    !Guid.TryParse(parts[1], out var guid))
                {
                    _logger.LogWarning("Invalid handshake from {RemoteEndpoint}: {Handshake}", remoteEndpoint, handshake);
                    return;
                }

                var serverBots = _bots.GetOrAdd(serverId, _ => new ConcurrentDictionary<string, BotUser>());

                if (serverBots.TryGetValue(guid.ToString(), out var existingBot))
                {
                    // Reconnection - update TCP connection
                    try { existingBot.TcpClient?.Close(); } catch { }
                    existingBot.TcpClient = client;
                    existingBot.LastInteracted = DateTime.UtcNow;
                    existingBot.LastReconnectSent = null; // Clear reconnect flag
                    bot = existingBot;

                    _logger.LogInformation("Bot {Guid} reconnected to server {ServerId}", guid, serverId);
                }
                else
                {
                    // New connection
                    bot = new BotUser(guid)
                    {
                        TcpClient = client,
                        ServerId = serverId,
                        LastInteracted = DateTime.UtcNow
                    };
                    serverBots[guid.ToString()] = bot;

                    _logger.LogInformation("New bot {Guid} connected to server {ServerId}", guid, serverId);
                }

                _ = Task.Run(() => SaveBotState());

                // Send initial response to confirm connection
                await SendStringResponse(stream, "CONNECTION_CONFIRMED");

                // Simplified message loop - handle messages and respond to health checks
                while (!token.IsCancellationRequested && client.Connected)
                {
                    var line = await ReceiveStringAsync(stream, token);
                    if (string.IsNullOrEmpty(line)) break;

                    bot.LastInteracted = DateTime.UtcNow;

                    if (line.StartsWith("HEALTHCHECK:"))
                    {
                        try
                        {
                            await SendStringResponse(stream, "HEALTHCHECK_RESPONSE:OK");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug("Failed to respond to health check from bot {Guid}: {Error}", guid, ex.Message);
                            break;
                        }
                    }
                    else if (!line.StartsWith("KEEPALIVE:"))
                    {
                        _logger.LogDebug("Bot {Guid} message: {Message}", guid, line);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Client error for bot {Guid}", bot?.Guid);
            }
            finally
            {
                try
                {
                    client.Close();
                    client.Dispose();
                }
                catch { }

                if (bot != null)
                {
                    _logger.LogDebug("TCP disconnected for bot {Guid}", bot.Guid);
                    _ = Task.Run(() => SaveBotState());
                }
            }
        }

        private async Task SendStringResponse(NetworkStream stream, string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthPrefix);

                await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
            }
            catch
            {
                throw;
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
                if (length <= 0 || length > 1024 * 1024) return null;

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
            catch
            {
                return null;
            }
        }

        public async Task StartAsync(CancellationToken token = default)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    _listener.Start();
                    _logger.LogInformation("Socket server started on port {Port}", ((IPEndPoint)_listener.LocalEndpoint).Port);

                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            var client = await _listener.AcceptTcpClientAsync(token);
                            _logger.LogDebug("New client connected from {RemoteEndpoint}", client.Client.RemoteEndPoint);
                            _ = HandleClientAsync(client, token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error accepting client");
                            await Task.Delay(1000, token);
                        }
                    }
                }
                catch (Exception ex) when (!token.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Listener crashed, retrying in 5 seconds...");
                    await Task.Delay(5000, token);
                }
                finally
                {
                    try { _listener.Stop(); } catch { }
                }
            }
        }

        public List<BotServerStats> GetAllServersStats()
        {
            return _bots.Keys.Select(serverId => GetBotStats(serverId)).ToList();
        }

        public async Task SendCommandToAll(long serverId, BotCommand command)
        {
            if (!_bots.TryGetValue(serverId, out var botsForServer))
            {
                _logger.LogWarning("No bots found for server {ServerId}", serverId);
                return;
            }

            var bots = botsForServer.Values.Where(b => b.TcpClient?.Connected == true).ToList();

            if (!bots.Any())
            {
                _logger.LogWarning("No connected bots available for server {ServerId} (Total registered: {TotalCount})",
                    serverId, botsForServer.Count);
                return;
            }

            var successCount = 0;
            foreach (var bot in bots)
            {
                try
                {
                    await SendLengthedMessage(command, bot);
                    bot.LastCommand = DateTime.UtcNow;
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send command to bot {Guid}", bot.Guid);
                    try { bot.TcpClient?.Close(); } catch { }
                }
            }

            _logger.LogInformation("Command sent to {SuccessCount}/{TotalCount} bots on server {ServerId}",
                successCount, bots.Count, serverId);
        }

        public BotServerStats GetBotStats(long serverId)
        {
            if (!_bots.TryGetValue(serverId, out var botsForServer))
            {
                return new BotServerStats
                {
                    ServerId = serverId,
                    ConnectedBots = 0,
                    TotalBots = 0,
                    ActiveBots = 0,
                    Bots = new List<BotStatus>()
                };
            }

            var allBots = botsForServer.Values.ToList();
            var now = DateTime.UtcNow;

            var botStatuses = allBots.Select(b => new BotStatus
            {
                Guid = b.Guid,
                SteamId = b.SteamId ?? "Unknown",
                IsConnected = b.TcpClient?.Connected == true,
                LastPinged = b.LastPinged,
                LastInteracted = b.LastInteracted,
                LastCommand = b.LastCommand,
                LastReconnectSent = b.LastReconnectSent,
                IsActive = b.LastPinged.HasValue && (now - b.LastPinged.Value).TotalMinutes < 10,
                MinutesSinceLastPing = b.LastPinged.HasValue ? (now - b.LastPinged.Value).TotalMinutes : null,
                MinutesSinceLastInteraction = (now - b.LastInteracted).TotalMinutes,
                RestoredFromState = b.TcpClient == null && b.LastInteracted < now.AddMinutes(-10) // Indicates restored from file
            }).ToList();

            return new BotServerStats
            {
                ServerId = serverId,
                TotalBots = allBots.Count,
                ConnectedBots = allBots.Count(b => b.TcpClient?.Connected == true),
                ActiveBots = allBots.Count(b => b.LastPinged.HasValue && (now - b.LastPinged.Value).TotalMinutes < 5),
                Bots = botStatuses
            };
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
                    bot.LastCommand = DateTime.UtcNow;
                    _logger.LogDebug("Command sent to specific bot {Guid} on server {ServerId}", bot.Guid, serverId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send command to bot {Guid}", bot.Guid);
                    try { bot.TcpClient?.Close(); } catch { }
                }
            }
            else
            {
                _logger.LogWarning("Bot {Guid} not found or not connected on server {ServerId}", guid, serverId);
            }
        }

        // Send command methods (unchanged)
        public async Task SendCommandAsync(long serverId, BotCommand command)
        {
            if (!_bots.TryGetValue(serverId, out var botsForServer))
            {
                _logger.LogWarning("No bots for server {ServerId}", serverId);
                return;
            }

            var bot = botsForServer.Values
                .Where(b => b.TcpClient?.Connected == true)
                .OrderByDescending(b => b.LastPinged ?? DateTime.MinValue)
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
                    _logger.LogWarning(ex, "Failed to send command to bot {Guid}", bot.Guid);
                    try { bot.TcpClient?.Close(); } catch { }
                }
            }
            else
            {
                _logger.LogWarning("No connected bots for server {ServerId}", serverId);
            }
        }

        public List<BotUser> GetBots(long serverId) =>
            _bots.TryGetValue(serverId, out var botsForServer)
                ? botsForServer.Values.ToList()
                : new List<BotUser>();

        public bool IsBotConnected(long serverId) =>
            _bots.TryGetValue(serverId, out var botsForServer) &&
            botsForServer.Values.Any(b =>
                b.TcpClient?.Connected == true ||
                (b.LastPinged.HasValue && (DateTime.UtcNow - b.LastPinged.Value) < PING_TIMEOUT));

        public void BotPingUpdate(long serverId, Guid guid, string steamId)
        {
            if (_bots.TryGetValue(serverId, out var botsForServer) &&
                botsForServer.TryGetValue(guid.ToString(), out var bot))
            {
                var stateChanged = false;

                if (bot.SteamId != steamId)
                {
                    bot.SteamId = steamId;
                    stateChanged = true;
                }

                bot.LastPinged = DateTime.UtcNow;

                if (stateChanged)
                {
                    _ = Task.Run(() => SaveBotState());
                }
            }
            else
            {
                _logger.LogWarning("Ping for unknown bot {Guid} on server {ServerId}", guid, serverId);
            }
        }
    }
}