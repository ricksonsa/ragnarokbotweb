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
    private System.Windows.Forms.Timer _keepAliveTimer;
    private System.Windows.Forms.Timer _connectionHealthTimer;

    private DateTime _lastMessageReceived = DateTime.UtcNow;
    private readonly System.Windows.Forms.Timer _inactivityTimer;
    private readonly TimeSpan _inactivityTimeout = TimeSpan.FromMinutes(5);
    private readonly System.Windows.Forms.Timer _pingTimer;

    private volatile bool _isDisconnecting = false;
    private CancellationTokenSource? _currentConnectionCts;

    public string BotId => _botId;
    public bool IsConnected => _client?.Connected == true;

    public event EventHandler<BotCommand>? OnMessageReceived;

    public WebApi Remote { get; set; }

    public BotSocketClient(string host, int port, long serverId)
    {
        _host = host;
        _port = port;
        _serverId = serverId;

        // Generate persistent bot ID only once
        _botId = GeneratePersistentBotId();

        _keepAliveTimer = new();
        _keepAliveTimer.Interval = 60000;
        _keepAliveTimer.Tick += KeepAliveTimer_Tick;

        _connectionHealthTimer = new();
        _connectionHealthTimer.Interval = 10000; // Check connection health every 10s
        _connectionHealthTimer.Tick += ConnectionHealthTimer_Tick;

        _inactivityTimer = new();
        _inactivityTimer.Interval = 60000; // check every 1 min
        _inactivityTimer.Tick += InactivityTimer_Tick;

        _pingTimer = new();
        _pingTimer.Interval = 60000; // check every 1 min
        _pingTimer.Tick += PingTimer_Tick;
    }

    private async void PingTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            var result = await Remote.GetAsync<BotState>($"api/bots/{_botId}");

            lock (_clientLock)
            {
                if (_client == null || _isDisconnecting) return;

                // Check if the connection is truly dead
                try
                {
                    if (result == null || !result.Connected)
                    {
                        Logger.LogWrite($"Connection health check failed for Bot ID {_botId} - connection appears dead");
                        ForceReconnect();
                        return;
                    }

                    if (!result.GameActive)
                    {
                        OnMessageReceived?.Invoke(sender, new BotCommand().Reconnect());
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWrite($"Connection health check exception for Bot ID {_botId}: {ex.Message}");
                    ForceReconnect();
                    return;
                }
            }
        }
        catch (Exception)
        {
            return;
        }
    }

    private string GeneratePersistentBotId()
    {
        var botIdFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"botid_server_{_serverId}.txt");

        // Try to load existing bot ID
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
                Logger.LogWrite("Invalid Bot ID in file, generating new one");
            }
            catch (Exception ex)
            {
                Logger.LogWrite($"Failed to read Bot ID file: {ex.Message}. Generating new one.");
            }
        }

        // Generate new persistent bot ID
        var newBotId = Guid.NewGuid().ToString();

        try
        {
            File.WriteAllText(botIdFile, newBotId);
            Logger.LogWrite($"Generated and saved new persistent Bot ID: {newBotId}");
        }
        catch (Exception ex)
        {
            Logger.LogWrite($"Failed to save Bot ID to file: {ex.Message}. Using in-memory ID.");
        }

        return newBotId;
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
                        Logger.LogWrite($"Connection health check failed for Bot ID {_botId} - connection appears dead");
                        ForceReconnect();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWrite($"Connection health check exception for Bot ID {_botId}: {ex.Message}");
                ForceReconnect();
                return;
            }
        }
    }

    private void ForceReconnect()
    {
        if (_isDisconnecting) return;

        Logger.LogWrite($"Forcing reconnection for Bot ID {_botId} due to health check failure");

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
                $"Bot ID {_botId}: No commands from server for {idleTime.TotalMinutes:F1} minutes. Forcing reconnect...");

            ForceReconnect();
            return;
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
            // Send simple keepalive message with persistent bot ID
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
                // Use persistent bot ID - no need to generate new one
                Logger.LogWrite($"Attempting to connect with persistent Bot ID: {_botId}");

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

                await _client.ConnectAsync(_host, _port, localToken);

                Logger.LogWrite($"TCP connected successfully with persistent Bot ID: {_botId}");

                // Send handshake with persistent bot ID
                await SendStringMessageAsync(_client.GetStream(), $"{_serverId}:{_botId}", localToken);

                Logger.LogWrite($"Handshake sent for persistent Bot ID: {_botId}");

                _ = Task.Run(() => ListenAsync(localToken), localToken); // fire-and-forget listen loop
                return; // exit connect loop on success
            }
            catch (OperationCanceledException) when (localToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (!localToken.IsCancellationRequested && !_isDisconnecting)
            {
                Logger.LogWrite($"Connection failed with persistent Bot ID {_botId}: {ex.Message}. Retrying in 10s...");
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

        try
        {
            using var stream = _client.GetStream();
            stream.ReadTimeout = 60000;

            while (!token.IsCancellationRequested && !_isDisconnecting)
            {
                TcpClient? currentClient;
                lock (_clientLock) currentClient = _client;

                if (currentClient?.Connected != true)
                {
                    Logger.LogWrite($"Bot ID {_botId}: TCP connection lost during listen loop");
                    break;
                }

                var command = await ReceiveMessagePackAsync(stream, token);
                if (command == null)
                {
                    Logger.LogWrite($"Bot ID {_botId}: Connection lost or received invalid command.");
                    break;
                }

                _lastMessageReceived = DateTime.UtcNow;

                Logger.LogWrite($"Bot ID {_botId}: Command received: {command.Values?.Count ?? 0} command(s)");

                try { OnMessageReceived?.Invoke(this, command); }
                catch (Exception ex) { Logger.LogWrite($"Bot ID {_botId}: Error in message handler: {ex.Message}"); }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            Logger.LogWrite($"Bot ID {_botId}: Listen loop cancelled");
        }
        catch (Exception ex) when (!token.IsCancellationRequested && !_isDisconnecting)
        {
            Logger.LogWrite($"Bot ID {_botId}: Listen error: {ex.Message}");
        }
        finally
        {
            StopTimers();

            lock (_clientLock)
            {
                try { _client?.Close(); } catch { }
                _client = null;
            }

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

        Logger.LogWrite($"Bot ID {_botId}: TCP connection lost - attempting to reconnect...");

        while (!token.IsCancellationRequested && !_isDisconnecting)
        {
            try
            {
                await ConnectAsync(token);
                Logger.LogWrite($"Bot ID {_botId}: TCP reconnected successfully");
                return;
            }
            catch (Exception ex) when (!token.IsCancellationRequested && !_isDisconnecting)
            {
                Logger.LogWrite($"Bot ID {_botId}: TCP reconnect failed: {ex.Message}. Retrying in 10s...");
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
                    Logger.LogWrite($"Bot ID {_botId}: Connection closed while reading length header");
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
                Logger.LogWrite($"Bot ID {_botId}: Invalid message length: {length}");
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
                    Logger.LogWrite($"Bot ID {_botId}: Connection closed while reading message body");
                    return null; // Connection closed
                }
                offset += chunk;
            }

            try
            {
                var command = MessagePackSerializer.Deserialize<BotCommand>(data);
                Logger.LogWrite($"Bot ID {_botId}: Successfully received and deserialized command with {command?.Values?.Count ?? 0} values");
                return command;
            }
            catch
            {
                // Fallback: treat as UTF-8 string
                string text = Encoding.UTF8.GetString(data);
                Logger.LogWrite($"Bot ID {_botId}: Received non-command message: {text}");
                return null;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return null;
        }
        catch (IOException ioEx)
        {
            Logger.LogWrite($"Bot ID {_botId}: IO error during receive: {ioEx.Message}");
            return null;
        }
        catch (SocketException sockEx)
        {
            Logger.LogWrite($"Bot ID {_botId}: Socket error during receive: {sockEx.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogWrite($"Bot ID {_botId}: Unexpected receive error: {ex.Message} | {ex.InnerException?.Message} | {ex.StackTrace}");
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
            Logger.LogWrite($"Bot ID {_botId}: Send error: {ex.Message}");
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

        if (!_client?.Connected ?? true)
            _isDisconnecting = false;

        Logger.LogWrite($"Bot ID {_botId}: Disconnected");
    }
}