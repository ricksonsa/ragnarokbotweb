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

        public BotSocketServer(ILogger<BotSocketServer> logger, IOptions<AppSettings> options)
        {
            _listener = new TcpListener(IPAddress.Any, options.Value.SocketServerPort);
            _logger = logger;

            // Create state file path in application directory
            _stateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "bot_state.json");

            // Auto-save state every 5 minutes
            _stateSaveTimer = new System.Timers.Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
            _stateSaveTimer.Elapsed += (s, e) => SaveBotState();
            _stateSaveTimer.AutoReset = true;
            _stateSaveTimer.Enabled = true;

            // Load existing state on startup
            LoadBotState();
        }

        #region State Persistence

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

                    // Write to temp file first, then atomic rename
                    var tempFile = _stateFilePath + ".tmp";
                    File.WriteAllText(tempFile, json);

                    if (File.Exists(_stateFilePath))
                        File.Delete(_stateFilePath);

                    File.Move(tempFile, _stateFilePath);

                    var totalBots = stateData.Servers.Sum(s => s.Value.Count);
                    _logger.LogDebug("Bot state saved to {FilePath} - {TotalBots} bots across {ServerCount} servers",
                        _stateFilePath, totalBots, stateData.Servers.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save bot state to {FilePath}", _stateFilePath);
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
                        _logger.LogInformation("No bot state file found at {FilePath} - starting with empty state", _stateFilePath);
                        return;
                    }

                    var json = File.ReadAllText(_stateFilePath);
                    var stateData = JsonSerializer.Deserialize<BotServerState>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (stateData?.Servers == null)
                    {
                        _logger.LogWarning("Invalid bot state file format - starting with empty state");
                        return;
                    }

                    var totalBotsLoaded = 0;
                    var now = DateTime.UtcNow;

                    foreach (var serverPair in stateData.Servers)
                    {
                        var serverId = serverPair.Key;
                        var serverBots = _bots.GetOrAdd(serverId, _ => new ConcurrentDictionary<string, BotUser>());

                        foreach (var persistedBot in serverPair.Value)
                        {
                            // Only restore bots that were recently active (within last 2 hours)
                            var timeSinceLastInteraction = now - persistedBot.LastInteracted;
                            if (timeSinceLastInteraction.TotalHours <= 2)
                            {
                                var restoredBot = new BotUser(persistedBot.Guid)
                                {
                                    ServerId = persistedBot.ServerId,
                                    SteamId = persistedBot.SteamId,
                                    LastPinged = persistedBot.LastPinged,
                                    LastInteracted = persistedBot.LastInteracted,
                                    LastCommand = persistedBot.LastCommand,
                                    LastReconnectSent = persistedBot.LastReconnectSent,
                                    // TcpClient will be null until bot reconnects
                                    TcpClient = null
                                };

                                serverBots[persistedBot.Guid.ToString()] = restoredBot;
                                totalBotsLoaded++;

                                _logger.LogDebug("Restored bot {Guid} for server {ServerId} (SteamId: {SteamId}, LastInteracted: {LastInteracted})",
                                    persistedBot.Guid, serverId, persistedBot.SteamId ?? "Unknown", persistedBot.LastInteracted);
                            }
                            else
                            {
                                _logger.LogDebug("Skipped restoring old bot {Guid} (last interaction: {LastInteraction})",
                                    persistedBot.Guid, persistedBot.LastInteracted);
                            }
                        }
                    }

                    _logger.LogInformation("Loaded bot state from {FilePath} - restored {TotalBots} bots across {ServerCount} servers (saved at: {SavedAt})",
                        _stateFilePath, totalBotsLoaded, stateData.Servers.Count, stateData.SavedAt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load bot state from {FilePath} - starting with empty state", _stateFilePath);
            }
        }

        public void SaveBotStateOnShutdown()
        {
            try
            {
                _stateSaveTimer?.Stop();
                SaveBotState();
                _logger.LogInformation("Bot state saved on shutdown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving bot state on shutdown");
            }
        }

        #endregion

        private async Task GameMonitorLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), token); // Check every minute

                var now = DateTime.UtcNow;

                foreach (var serverPair in _bots.ToList())
                {
                    var serverId = serverPair.Key;
                    var serverBots = serverPair.Value;

                    foreach (var botPair in serverBots.ToList())
                    {
                        var bot = botPair.Value;

                        // Only remove bot if TCP disconnected AND no recent game activity for a LONG time
                        if (bot.TcpClient?.Connected != true)
                        {
                            // Only remove if bot has been offline for more than 2 hours (very long threshold)
                            if (!bot.LastPinged.HasValue || (now - bot.LastPinged.Value).TotalHours >= 2)
                            {
                                // Additional check: also verify no recent TCP interaction for over 1 hour
                                if ((now - bot.LastInteracted).TotalHours >= 1)
                                {
                                    _logger.LogWarning("Bot {Guid} has been offline for too long - removing from server {ServerId} (LastPing: {LastPing}, LastInteraction: {LastInteraction})",
                                        bot.Guid, serverId,
                                        bot.LastPinged?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never",
                                        bot.LastInteracted.ToString("yyyy-MM-dd HH:mm:ss"));

                                    RemoveBotFromCollection(serverId, bot.Guid.ToString());
                                }
                                else
                                {
                                    _logger.LogDebug("Bot {Guid} TCP disconnected but recent interaction - keeping in collection", bot.Guid);
                                }
                            }
                            else
                            {
                                _logger.LogDebug("Bot {Guid} TCP disconnected but recent game activity - keeping in collection", bot.Guid);
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
                                        try { bot.TcpClient?.Close(); } catch { }
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
                                        try { bot.TcpClient?.Close(); } catch { }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Separate method for safe bot removal - also triggers state save
        private void RemoveBotFromCollection(long serverId, string botGuid)
        {
            if (_bots.TryGetValue(serverId, out var botsForServer))
            {
                if (botsForServer.TryRemove(botGuid, out var removedBot))
                {
                    _logger.LogInformation("Removed Bot {Guid} from server {ServerId} collection", removedBot.Guid, serverId);
                    try
                    {
                        removedBot.TcpClient?.Dispose();
                    }
                    catch { }

                    // Save state after removal
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

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using var stream = client.GetStream();
            BotUser? bot = null;
            var remoteEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
            var isNewConnection = false;

            try
            {
                // --- Enhanced Handshake ---
                var handshake = await ReceiveStringAsync(stream, token);
                if (string.IsNullOrWhiteSpace(handshake))
                {
                    _logger.LogWarning("Empty handshake received from {RemoteEndpoint}", remoteEndpoint);
                    return;
                }

                var parts = handshake.Split(':', 2);
                if (parts.Length < 2)
                {
                    _logger.LogWarning("Invalid handshake format from {RemoteEndpoint}: {Handshake}", remoteEndpoint, handshake);
                    return;
                }

                if (!long.TryParse(parts[0], out var serverId))
                {
                    _logger.LogWarning("Invalid server ID in handshake from {RemoteEndpoint}: {ServerId}", remoteEndpoint, parts[0]);
                    return;
                }

                if (!Guid.TryParse(parts[1], out var guid))
                {
                    _logger.LogWarning("Invalid GUID in handshake from {RemoteEndpoint}: {Guid}", remoteEndpoint, parts[1]);
                    return;
                }

                var serverBots = _bots.GetOrAdd(serverId, _ => new ConcurrentDictionary<string, BotUser>());

                // Check if this is a reconnection of an existing bot
                if (serverBots.TryGetValue(guid.ToString(), out var existingBot))
                {
                    _logger.LogInformation("Bot {Guid} reconnecting to server {ServerId} - updating TCP connection (was connected: {WasConnected}, restored from state: {RestoredFromState})",
                        guid, serverId, existingBot.TcpClient?.Connected == true, existingBot.TcpClient == null);

                    // Close old connection if it exists
                    try
                    {
                        existingBot.TcpClient?.Close();
                        existingBot.TcpClient?.Dispose();
                    }
                    catch { }

                    // Update with new TCP connection but keep all other data
                    existingBot.TcpClient = client;
                    existingBot.LastInteracted = DateTime.UtcNow;
                    // Keep LastPinged, SteamId, and other data from previous connection or restored state
                    bot = existingBot;
                }
                else
                {
                    // New bot connection
                    bot = new BotUser(guid)
                    {
                        TcpClient = client,
                        ServerId = serverId,
                        LastInteracted = DateTime.UtcNow
                    };

                    serverBots[guid.ToString()] = bot;
                    isNewConnection = true;
                    _logger.LogInformation("New Bot {Guid} connected to server {ServerId} (Total bots: {Count})",
                        guid, serverId, serverBots.Count);
                }

                // Save state when new connection is established
                if (isNewConnection || bot.TcpClient != null)
                {
                    _ = Task.Run(() => SaveBotState());
                }

                // --- Message loop ---
                while (!token.IsCancellationRequested && client.Connected)
                {
                    var line = await ReceiveStringAsync(stream, token);
                    if (string.IsNullOrEmpty(line))
                    {
                        _logger.LogDebug("Bot {Guid} connection closed", guid);
                        break;
                    }

                    bot.LastInteracted = DateTime.UtcNow;

                    // Handle keepalive messages
                    if (line.StartsWith("KEEPALIVE:"))
                    {
                        _logger.LogDebug("Received keepalive from bot {Guid}", guid);
                        continue;
                    }

                    _logger.LogDebug("Received from bot {Guid}: {Message}", guid, line);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Client error for bot {Guid} from {RemoteEndpoint}", bot?.Guid, remoteEndpoint);
            }
            finally
            {
                // Don't remove the bot from collection - just close TCP and save state
                try
                {
                    client.Close();
                    client.Dispose();
                }
                catch { }

                if (bot != null)
                {
                    _logger.LogInformation("TCP connection closed for Bot {Guid} - bot remains in collection for potential reconnection", bot.Guid);
                    // Save state after connection closes
                    _ = Task.Run(() => SaveBotState());
                }
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

            // Prioritize bots with recent game ping and active TCP connection
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
                    try { bot.TcpClient?.Close(); } catch { }
                }
            }
            else
            {
                _logger.LogWarning("No connected bots available for server {ServerId} (Total bots: {TotalCount})",
                    serverId, botsForServer.Count);

                // Log details about why no bots are available
                foreach (var botStatus in botsForServer.Values)
                {
                    _logger.LogDebug("Bot {Guid}: TCP Connected = {Connected}, Last Interaction = {LastInteraction}",
                        botStatus.Guid,
                        botStatus.TcpClient?.Connected == true,
                        botStatus.LastInteracted.ToString("HH:mm:ss"));
                }
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

        // Enhanced BotPingUpdate to handle persistent bot IDs and trigger state save
        public void BotPingUpdate(long serverId, Guid guid, string steamId)
        {
            var stateChanged = false;

            if (_bots.TryGetValue(serverId, out var botsForServer) &&
                botsForServer.TryGetValue(guid.ToString(), out var bot))
            {
                // Check if SteamId changed
                if (bot.SteamId != steamId)
                {
                    bot.SteamId = steamId;
                    stateChanged = true;
                }

                bot.LastPinged = DateTime.UtcNow;
                stateChanged = true; // LastPinged always triggers state save

                _logger.LogDebug("Bot {Guid} ping updated via game chat on server {ServerId} (SteamID: {SteamId}, TCP Connected: {TcpConnected})",
                    guid, serverId, steamId, bot.TcpClient?.Connected == true);

                if (stateChanged)
                {
                    _ = Task.Run(() => SaveBotState());
                }
            }
            else
            {
                _logger.LogWarning("Received ping for unknown bot {Guid} on server {ServerId} (SteamID: {SteamId})",
                    guid, serverId, steamId);

                // Log what bots we DO have for debugging
                if (_bots.TryGetValue(serverId, out var serverBots) && serverBots.Any())
                {
                    var knownBots = string.Join(", ", serverBots.Select(kvp => $"{kvp.Key}({kvp.Value.SteamId ?? "NoSteam"})"));
                    _logger.LogWarning("Known bots for server {ServerId}: [{KnownBots}]", serverId, knownBots);
                }
                else
                {
                    _logger.LogWarning("No bots registered for server {ServerId}", serverId);
                }
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

        // Enhanced method to get detailed bot statistics
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
                IsActive = b.LastPinged.HasValue && (now - b.LastPinged.Value).TotalMinutes < 5,
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

        // Get stats for all servers
        public List<BotServerStats> GetAllServersStats()
        {
            return _bots.Keys.Select(serverId => GetBotStats(serverId)).ToList();
        }
    }

    // Supporting classes for state persistence
    public class BotServerState
    {
        public DateTime SavedAt { get; set; }
        public Dictionary<long, List<PersistedBotUser>> Servers { get; set; } = new();
    }

    public class PersistedBotUser
    {
        public Guid Guid { get; set; }
        public long ServerId { get; set; }
        public string? SteamId { get; set; }
        public DateTime? LastPinged { get; set; }
        public DateTime LastInteracted { get; set; }
        public DateTime? LastCommand { get; set; }
        public DateTime? LastReconnectSent { get; set; }
    }

    // Supporting classes for enhanced bot statistics
    public class BotServerStats
    {
        public long ServerId { get; set; }
        public int TotalBots { get; set; }
        public int ConnectedBots { get; set; }
        public int ActiveBots { get; set; }
        public List<BotStatus> Bots { get; set; } = new();
    }

    public class BotStatus
    {
        public Guid Guid { get; set; }
        public string SteamId { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastPinged { get; set; }
        public DateTime LastInteracted { get; set; }
        public DateTime? LastCommand { get; set; }
        public DateTime? LastReconnectSent { get; set; }
        public double? MinutesSinceLastPing { get; set; }
        public double MinutesSinceLastInteraction { get; set; }
        public bool RestoredFromState { get; set; }
    }
}