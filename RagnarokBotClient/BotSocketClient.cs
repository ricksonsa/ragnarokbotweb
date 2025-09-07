using MessagePack;
using RagnarokBotClient;
using Shared.Models;
using System.Net.Sockets;
using System.Text;

public class BotSocketClient
{
    private readonly string _host;
    private readonly int _port;
    private string _botId = string.Empty;
    private readonly long _serverId;
    private readonly object _clientLock = new object();

    private TcpClient? _client;
    private System.Windows.Forms.Timer _connectionHealthTimer;

    private DateTime _lastMessageReceived = DateTime.UtcNow;
    private volatile bool _isDisconnecting = false;
    private CancellationTokenSource? _currentConnectionCts;

    // Simplified timeouts
    private static readonly TimeSpan INACTIVITY_TIMEOUT = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan HEALTH_CHECK_INTERVAL = TimeSpan.FromSeconds(30); // Reduced from 10s
    private static readonly TimeSpan RECONNECT_COOLDOWN = TimeSpan.FromSeconds(15); // Reduced from 30s

    private DateTime _lastReconnectAttempt = DateTime.MinValue;
    private DateTime _lastHealthCheck = DateTime.UtcNow;

    public string BotId => _botId;
    public bool IsConnected => _client?.Connected == true;

    public event EventHandler<BotCommand>? OnMessageReceived;
    public WebApi Remote { get; set; }

    public BotSocketClient(string host, int port, long serverId)
    {
        _host = host;
        _port = port;
        _serverId = serverId;
        _botId = GeneratePersistentBotId();

        // Single timer for connection health
        _connectionHealthTimer = new();
        _connectionHealthTimer.Interval = (int)HEALTH_CHECK_INTERVAL.TotalMilliseconds;
        _connectionHealthTimer.Tick += ConnectionHealthTimer_Tick;
        _connectionHealthTimer.Start();
    }

    private string GeneratePersistentBotId()
    {
        var botIdFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"botid_server_{_serverId}.txt");

        if (File.Exists(botIdFile))
        {
            try
            {
                var existingId = File.ReadAllText(botIdFile).Trim();
                if (!string.IsNullOrEmpty(existingId) && Guid.TryParse(existingId, out _))
                {
                    Logger.LogWrite($"Using existing Bot ID: {existingId}");
                    return existingId;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWrite($"Failed to read Bot ID file: {ex.Message}. Generating new one.");
            }
        }

        var newBotId = Guid.NewGuid().ToString();
        try
        {
            File.WriteAllText(botIdFile, newBotId);
            Logger.LogWrite($"Generated new Bot ID: {newBotId}");
        }
        catch (Exception ex)
        {
            Logger.LogWrite($"Failed to save Bot ID: {ex.Message}");
        }

        return newBotId;
    }

    private async void ConnectionHealthTimer_Tick(object? sender, EventArgs e)
    {
        if (_isDisconnecting) return;

        var now = DateTime.UtcNow;

        var timeSinceConnection = now - _lastHealthCheck;
        if (timeSinceConnection < TimeSpan.FromMinutes(3))
        {
            return; // Skip health checks for the first 3 minutes after connection
        }

        // Fix: Only check inactivity if we've sent health checks but received no responses
        // Instead of checking _lastMessageReceived, track health check responses
        if ((now - _lastMessageReceived) > INACTIVITY_TIMEOUT &&
            (now - _lastHealthCheck) > TimeSpan.FromMinutes(1)) // Ensure we've had time to receive responses
        {
            Logger.LogWrite($"Bot {_botId}: No server activity for {(now - _lastMessageReceived).TotalMinutes:F1}min - reconnecting");
            ForceReconnect();
            return;
        }

        if (_client?.Connected == true && (now - _lastHealthCheck) > TimeSpan.FromSeconds(30))
        {
            try
            {
                await SendStringMessageAsync(_client.GetStream(), $"HEALTHCHECK:{_serverId}:{_botId}");
                _lastHealthCheck = now;
            }
            catch
            {
                Logger.LogWrite($"Bot {_botId}: Health check failed - reconnecting");
                ForceReconnect();
            }
        }
    }

    public void ForceReconnect()
    {
        if (_isDisconnecting) return;

        // Prevent rapid reconnection attempts
        if (DateTime.UtcNow - _lastReconnectAttempt < RECONNECT_COOLDOWN)
        {
            return;
        }

        _lastReconnectAttempt = DateTime.UtcNow;
        Logger.LogWrite($"Bot {_botId}: Forcing reconnection");

        try
        {
            _currentConnectionCts?.Cancel();
            _client?.Close();
        }
        catch { }

        _client = null;
        _ = Task.Run(() => ConnectAsync(CancellationToken.None));
    }

    public async Task ConnectAsync(CancellationToken token = default)
    {
        if (_isDisconnecting) return;

        _currentConnectionCts?.Cancel();
        _currentConnectionCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        var localToken = _currentConnectionCts.Token;

        while (!localToken.IsCancellationRequested && !_isDisconnecting)
        {
            try
            {
                Logger.LogWrite($"Bot {_botId}: Connecting to {_host}:{_port}");

                lock (_clientLock)
                {
                    _client?.Dispose();
                    _client = new TcpClient();

                    // Simplified TCP configuration
                    _client.ReceiveTimeout = 60000;
                    _client.SendTimeout = 30000;
                    _client.NoDelay = true;
                }

                await _client.ConnectAsync(_host, _port, localToken);
                Logger.LogWrite($"Bot {_botId}: TCP connected");

                // Send handshake
                await SendStringMessageAsync(_client.GetStream(), $"{_serverId}:{_botId}", localToken);
                Logger.LogWrite($"Bot {_botId}: Handshake sent");

                // Fix: Reset activity trackers and give connection time to establish
                var now = DateTime.UtcNow;
                _lastMessageReceived = now;
                _lastHealthCheck = now;

                // Fix: Wait a bit before starting health checks to avoid immediate reconnection triggers
                await Task.Delay(2000, localToken); // Wait 2 seconds

                _ = Task.Run(() => ListenAsync(localToken), localToken);
                return;
            }
            catch (OperationCanceledException) when (localToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (!localToken.IsCancellationRequested && !_isDisconnecting)
            {
                Logger.LogWrite($"Bot {_botId}: Connection failed: {ex.Message}. Retrying in 10s...");
                try
                {
                    await Task.Delay(10000, localToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task ListenAsync(CancellationToken token)
    {
        if (_client == null || _isDisconnecting) return;

        try
        {
            using var stream = _client.GetStream();
            stream.ReadTimeout = 60000;

            Logger.LogWrite($"Bot {_botId}: Listen loop started");

            while (!token.IsCancellationRequested && !_isDisconnecting)
            {
                TcpClient? currentClient;
                lock (_clientLock) currentClient = _client;

                if (currentClient?.Connected != true)
                {
                    Logger.LogWrite($"Bot {_botId}: Connection lost in listen loop");
                    break;
                }

                // Read the raw message first, then determine how to process it
                var messageData = await ReceiveRawMessageAsync(stream, token);
                if (messageData == null)
                {
                    Logger.LogWrite($"Bot {_botId}: Connection lost - no data received");
                    break;
                }

                _lastMessageReceived = DateTime.UtcNow;

                // Try to deserialize as MessagePack first
                BotCommand? command = null;
                bool isStringMessage = false;
                string? stringMessage = null;

                try
                {
                    command = MessagePackSerializer.Deserialize<BotCommand>(messageData);
                    Logger.LogWrite($"Bot {_botId}: MessagePack command received with {command.Values?.Count ?? 0} values");
                }
                catch
                {
                    // Not MessagePack, treat as string
                    isStringMessage = true;
                    stringMessage = Encoding.UTF8.GetString(messageData);
                }

                // Process the message based on its type
                if (isStringMessage)
                {
                    // Handle string messages (health check responses, confirmations, etc.)
                    HandleStringMessage(stringMessage);
                }
                else if (command != null)
                {
                    // Handle BotCommand messages
                    try
                    {
                        OnMessageReceived?.Invoke(this, command);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWrite($"Bot {_botId}: Error in message handler: {ex.Message}");
                    }
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            Logger.LogWrite($"Bot {_botId}: Listen loop cancelled");
        }
        catch (Exception ex) when (!token.IsCancellationRequested && !_isDisconnecting)
        {
            Logger.LogWrite($"Bot {_botId}: Listen error: {ex.Message}");
        }
        finally
        {
            lock (_clientLock)
            {
                try { _client?.Close(); } catch { }
                _client = null;
            }

            if (!token.IsCancellationRequested && !_isDisconnecting)
            {
                Logger.LogWrite($"Bot {_botId}: Connection lost - will reconnect");
                _ = Task.Run(() => ConnectAsync(token));
            }
        }
    }

    private async Task<byte[]?> ReceiveRawMessageAsync(NetworkStream stream, CancellationToken token)
    {
        try
        {
            // Read length header
            var lengthBuffer = new byte[4];
            int totalRead = 0;
            while (totalRead < 4)
            {
                int read = await stream.ReadAsync(lengthBuffer, totalRead, 4 - totalRead, token);
                if (read == 0) return null; // Connection closed
                totalRead += read;
            }

            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBuffer);

            int length = BitConverter.ToInt32(lengthBuffer, 0);
            if (length <= 0 || length > 10 * 1024 * 1024) // 10MB limit
                return null;

            // Read message data
            var data = new byte[length];
            int offset = 0;
            while (offset < length)
            {
                int chunk = await stream.ReadAsync(data, offset, length - offset, token);
                if (chunk == 0) return null; // Connection closed
                offset += chunk;
            }

            return data;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogWrite($"Bot {_botId}: Receive error: {ex.Message}");
            return null;
        }
    }

    // New method to handle string messages
    private void HandleStringMessage(string? message)
    {
        if (string.IsNullOrEmpty(message)) return;

        if (message.StartsWith("CONNECTION_CONFIRMED"))
        {
            Logger.LogWrite($"Bot {_botId}: Server confirmed connection");
        }
        else if (message.StartsWith("HEALTHCHECK_RESPONSE:"))
        {
            Logger.LogWrite($"Bot {_botId}: Health check response received");
        }
        else if (message.StartsWith("PONG:"))
        {
            Logger.LogWrite($"Bot {_botId}: Health check response received");
        }
        else
        {
            Logger.LogWrite($"Bot {_botId}: Unknown string message: {message}");
        }
    }

    public async Task<BotCommand?> ReceiveMessagePackAsync(NetworkStream stream, CancellationToken token)
    {
        try
        {
            // Read length header
            var lengthBuffer = new byte[4];
            int totalRead = 0;
            while (totalRead < 4)
            {
                int read = await stream.ReadAsync(lengthBuffer, totalRead, 4 - totalRead, token);
                if (read == 0) return null; // Connection closed
                totalRead += read;
            }

            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBuffer);

            int length = BitConverter.ToInt32(lengthBuffer, 0);
            if (length <= 0 || length > 10 * 1024 * 1024) // 10MB limit
                return null;

            // Read message data
            var data = new byte[length];
            int offset = 0;
            while (offset < length)
            {
                int chunk = await stream.ReadAsync(data, offset, length - offset, token);
                if (chunk == 0) return null; // Connection closed
                offset += chunk;
            }

            // Fix: Always update _lastMessageReceived when we receive ANY data
            _lastMessageReceived = DateTime.UtcNow;

            try
            {
                var command = MessagePackSerializer.Deserialize<BotCommand>(data);
                return command;
            }
            catch
            {
                // Treat as string message (keepalive, health check response, etc.)
                string text = Encoding.UTF8.GetString(data);
                Logger.LogWrite($"Bot {_botId}: Received text: {text}");

                // Fix: Recognize health check responses
                if (text.StartsWith("HEALTHCHECK_RESPONSE:") || text.StartsWith("PONG:"))
                {
                    // This is a keep-alive response, connection is healthy
                    return null;
                }

                return null;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogWrite($"Bot {_botId}: Receive error: {ex.Message}");
            return null;
        }
    }

    public async Task SendStringMessageAsync(NetworkStream stream, string message, CancellationToken token = default)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthPrefix);

            await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length, token);
            await stream.WriteAsync(data, 0, data.Length, token);
            await stream.FlushAsync(token);
        }
        catch (Exception ex)
        {
            Logger.LogWrite($"Bot {_botId}: Send error: {ex.Message}");
            throw;
        }
    }

    public void Disconnect()
    {
        _isDisconnecting = true;

        _currentConnectionCts?.Cancel();
        _connectionHealthTimer?.Stop();

        lock (_clientLock)
        {
            try
            {
                _client?.Close();
                _client?.Dispose();
            }
            catch { }
            _client = null;
        }

        _isDisconnecting = false;
        Logger.LogWrite($"Bot {_botId}: Disconnected");
    }
}