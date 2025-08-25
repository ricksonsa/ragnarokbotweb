using MessagePack;
using RagnarokBotClient;
using Shared.Models;
using System.Net.Sockets;
using System.Text;

public class BotSocketClient
{
    private readonly string _host;
    private readonly int _port;
    private TcpClient? _client;
    private string _botId = string.Empty;
    private readonly long _serverId;
    private readonly object _botIdLock = new object();
    private readonly object _clientLock = new object();

    private System.Windows.Forms.Timer _keepAliveTimer;
    private System.Windows.Forms.Timer _connectionHealthTimer;

    private DateTime _lastMessageReceived = DateTime.UtcNow;
    private readonly System.Windows.Forms.Timer _inactivityTimer;
    private readonly TimeSpan _inactivityTimeout = TimeSpan.FromMinutes(5);

    private volatile bool _isDisconnecting = false;
    private CancellationTokenSource? _currentConnectionCts;

    public string BotId => _botId;
    public bool IsConnected => _client?.Connected == true;

    public event EventHandler<BotCommand>? OnMessageReceived;

    public BotSocketClient(string host, int port, long serverId)
    {
        _host = host;
        _port = port;
        _serverId = serverId;

        _keepAliveTimer = new();
        _keepAliveTimer.Interval = 30000; // 30s keepalive
        _keepAliveTimer.Tick += KeepAliveTimer_Tick;

        _connectionHealthTimer = new();
        _connectionHealthTimer.Interval = 10000; // Check connection health every 10s
        _connectionHealthTimer.Tick += ConnectionHealthTimer_Tick;

        _inactivityTimer = new();
        _inactivityTimer.Interval = 60000; // check every 1 min
        _inactivityTimer.Tick += InactivityTimer_Tick;
    }

    private async void ConnectionHealthTimer_Tick(object? sender, EventArgs e)
    {
        lock (_clientLock)
        {
            if (_client == null || _isDisconnecting) return;

            // Check if the connection is actually dead
            try
            {
                if (_client.Client.Poll(0, SelectMode.SelectRead))
                {
                    byte[] buff = new byte[1];
                    if (_client.Client.Receive(buff, SocketFlags.Peek) == 0)
                    {
                        Logger.LogWrite("Connection health check failed - connection appears dead");
                        ForceReconnect();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWrite($"Connection health check exception: {ex.Message}");
                ForceReconnect();
                return;
            }
        }
    }

    private void ForceReconnect()
    {
        if (_isDisconnecting) return;

        Logger.LogWrite("Forcing reconnection due to health check failure - will generate new Bot ID");

        try
        {
            _currentConnectionCts?.Cancel();
            _client?.Close();
        }
        catch { }

        _client = null;
        _ = Task.Run(() => ReconnectLoopAsync(CancellationToken.None));
    }

    private async void InactivityTimer_Tick(object? sender, EventArgs e)
    {
        var idleTime = DateTime.UtcNow - _lastMessageReceived;
        if (idleTime > _inactivityTimeout)
        {
            Logger.LogWrite(
                $"No commands from server for {idleTime.TotalMinutes:F1} minutes. Forcing reconnect with new Bot ID...");

            ForceReconnect();
        }
    }

    private async void KeepAliveTimer_Tick(object? sender, EventArgs e)
    {
        lock (_clientLock)
        {
            if (_client?.Connected != true || _isDisconnecting) return;
        }

        try
        {
            // Send simple keepalive message with current bot ID
            await SendStringMessageAsync(_client!.GetStream(), $"KEEPALIVE:{_serverId}:{_botId}");
        }
        catch (Exception ex)
        {
            Logger.LogWrite($"Keepalive failed for Bot ID {_botId}: {ex.Message}");
            ForceReconnect();
        }
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
                // Generate new bot ID for every connection attempt
                var newBotId = Guid.NewGuid().ToString();

                lock (_botIdLock)
                {
                    _botId = newBotId;
                }

                lock (_clientLock)
                {
                    _client?.Dispose();
                    _client = new TcpClient();

                    // Configure TCP settings for better reliability
                    _client.ReceiveTimeout = 60000; // 1 minute
                    _client.SendTimeout = 30000;    // 30 seconds
                    _client.NoDelay = true;         // Disable Nagle's algorithm

                    // Configure socket-level keepalive
                    _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    _client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 30);
                    _client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 10);
                    _client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);
                }

                Logger.LogWrite($"Attempting to connect with new Bot ID: {_botId}");
                await _client.ConnectAsync(_host, _port, localToken);

                Logger.LogWrite($"TCP connected successfully with Bot ID: {_botId}");

                // Send handshake with new bot ID
                await SendStringMessageAsync(_client.GetStream(), $"{_serverId}:{_botId}", localToken);

                Logger.LogWrite($"Handshake sent for Bot ID: {_botId}");

                _ = Task.Run(() => ListenAsync(localToken), localToken); // fire-and-forget listen loop
                return; // exit connect loop on success
            }
            catch (OperationCanceledException) when (localToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (!localToken.IsCancellationRequested && !_isDisconnecting)
            {
                Logger.LogWrite($"Connection failed with Bot ID {_botId}: {ex.Message}. Retrying with new ID in 10s...");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), localToken);
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

        _keepAliveTimer.Start();
        _inactivityTimer.Start();
        _connectionHealthTimer.Start();

        try
        {
            using var stream = _client.GetStream();
            stream.ReadTimeout = 60000; // 1 minute read timeout

            while (!token.IsCancellationRequested && !_isDisconnecting)
            {
                TcpClient? currentClient;
                lock (_clientLock)
                {
                    currentClient = _client;
                }

                if (currentClient?.Connected != true)
                {
                    Logger.LogWrite("TCP connection lost during listen loop");
                    break;
                }

                // Receive MessagePack commands from server
                var command = await ReceiveMessagePackAsync(stream, token);
                if (command == null)
                {
                    Logger.LogWrite("Connection lost or received invalid command.");
                    break;
                }

                _lastMessageReceived = DateTime.UtcNow; // reset idle timer

                Logger.LogWrite($"Command received: {command.Values?.Count ?? 0} command(s)");

                // Handle reconnect command specially - don't break connection
                bool hasReconnectCommand = command.Values?.Any(cmd => cmd.Type == Shared.Enums.ECommandType.Reconnect) == true;

                if (hasReconnectCommand)
                {
                    Logger.LogWrite("Received reconnect command - will rejoin game server");
                }

                // Always invoke the event for command processing
                try
                {
                    OnMessageReceived?.Invoke(this, command);
                }
                catch (Exception ex)
                {
                    Logger.LogWrite($"Error in message handler: {ex.Message}");
                }

                // For reconnect commands, we DON'T break the TCP connection
                // The bot will handle rejoining the game server while keeping TCP alive
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            Logger.LogWrite("Listen loop cancelled");
        }
        catch (Exception ex) when (!token.IsCancellationRequested && !_isDisconnecting)
        {
            Logger.LogWrite($"Listen error: {ex.Message}");
        }
        finally
        {
            StopTimers();

            lock (_clientLock)
            {
                try { _client?.Close(); } catch { }
                _client = null;
            }

            // Only reconnect if we haven't been cancelled and not disconnecting
            if (!token.IsCancellationRequested && !_isDisconnecting)
            {
                _ = Task.Run(() => ReconnectLoopAsync(token));
            }
        }
    }

    private void StopTimers()
    {
        try { _keepAliveTimer.Stop(); } catch { }
        try { _inactivityTimer.Stop(); } catch { }
        try { _connectionHealthTimer.Stop(); } catch { }
    }

    private async Task ReconnectLoopAsync(CancellationToken token)
    {
        if (_isDisconnecting) return;

        Logger.LogWrite("TCP connection lost - attempting to reconnect with new Bot ID...");

        while (!token.IsCancellationRequested && !_isDisconnecting)
        {
            try
            {
                await ConnectAsync(token);
                Logger.LogWrite($"TCP reconnected successfully with new Bot ID: {_botId}");
                return;
            }
            catch (Exception ex) when (!token.IsCancellationRequested && !_isDisconnecting)
            {
                Logger.LogWrite($"TCP reconnect failed: {ex.Message}. Retrying with new Bot ID in 10s...");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    public async Task<BotCommand?> ReceiveMessagePackAsync(NetworkStream stream, CancellationToken token)
    {
        try
        {
            var lengthBuffer = new byte[4];
            int totalRead = 0;

            // Ensure we read exactly 4 bytes for length with timeout
            while (totalRead < 4)
            {
                int read = await stream.ReadAsync(lengthBuffer, totalRead, 4 - totalRead, token);
                if (read == 0)
                {
                    Logger.LogWrite("Connection closed while reading length header");
                    return null; // Connection closed
                }
                totalRead += read;
            }

            // Convert from big-endian
            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBuffer);

            int length = BitConverter.ToInt32(lengthBuffer, 0);
            if (length <= 0 || length > 10 * 1024 * 1024) // 10MB limit
            {
                Logger.LogWrite($"Invalid message length: {length}");
                return null;
            }

            // Read the message data with proper buffering
            var data = new byte[length];
            int offset = 0;
            while (offset < length)
            {
                int chunk = await stream.ReadAsync(data, offset, length - offset, token);
                if (chunk == 0)
                {
                    Logger.LogWrite("Connection closed while reading message body");
                    return null; // Connection closed
                }
                offset += chunk;
            }

            try
            {
                var command = MessagePackSerializer.Deserialize<BotCommand>(data);
                Logger.LogWrite($"Successfully received and deserialized command with {command?.Values?.Count ?? 0} values");
                return command;
            }
            catch
            {
                // Fallback: treat as UTF-8 string
                string text = Encoding.UTF8.GetString(data);
                Logger.LogWrite($"Received non-command message: {text}");
                return null;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return null;
        }
        catch (IOException ioEx)
        {
            Logger.LogWrite($"IO error during receive: {ioEx.Message}");
            return null;
        }
        catch (SocketException sockEx)
        {
            Logger.LogWrite($"Socket error during receive: {sockEx.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogWrite($"Unexpected receive error: {ex.Message} | {ex.InnerException?.Message} | {ex.StackTrace}");
            return null;
        }
    }

    public async Task SendStringMessageAsync(NetworkStream stream, string message, CancellationToken token = default)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

            // Convert to big-endian
            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthPrefix);

            await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length, token);
            await stream.WriteAsync(data, 0, data.Length, token);
            await stream.FlushAsync(token);
        }
        catch (Exception ex)
        {
            Logger.LogWrite($"Send error: {ex.Message}");
            throw;
        }
    }

    public void Disconnect()
    {
        _isDisconnecting = true;

        _currentConnectionCts?.Cancel();
        StopTimers();

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
    }
}