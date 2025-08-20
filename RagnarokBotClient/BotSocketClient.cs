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

    private System.Windows.Forms.Timer _keepAliveTimer;

    public string BotId => _botId;

    public event EventHandler<BotCommand>? OnMessageReceived;

    public BotSocketClient(string host, int port, long serverId)
    {
        _host = host;
        _port = port;
        _serverId = serverId;
        _keepAliveTimer = new();
        _keepAliveTimer.Interval = 30000; // Send keepalive every 30 seconds
        _keepAliveTimer.Tick += KeepAliveTimer_Tick;
    }

    private async void KeepAliveTimer_Tick(object? sender, EventArgs e)
    {
        if (_client?.Connected == true)
        {
            try
            {
                // Send simple keepalive message
                await SendStringMessageAsync(_client.GetStream(), $"KEEPALIVE:{_serverId}:{_botId}");
            }
            catch (Exception ex)
            {
                Logger.LogWrite($"Keepalive failed: {ex.Message}");
                // Don't throw here, let the main listen loop handle disconnection
            }
        }
    }

    public async Task ConnectAsync(CancellationToken token = default)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                lock (_botIdLock)
                {
                    _botId = Guid.NewGuid().ToString();
                }

                _client = new TcpClient();
                await _client.ConnectAsync(_host, _port, token);

                Logger.LogWrite($"Connected to server with Bot ID: {_botId}");

                // Send handshake
                await SendStringMessageAsync(_client.GetStream(), $"{_serverId}:{_botId}", token);

                _ = ListenAsync(token); // fire-and-forget listen loop
                return; // exit connect loop on success
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                Logger.LogWrite($"Connection failed: {ex.Message}. Retrying in 10s...");
                await Task.Delay(TimeSpan.FromSeconds(10), token);
            }
        }
    }

    private async Task ListenAsync(CancellationToken token)
    {
        if (_client == null) return;

        _keepAliveTimer.Start();

        try
        {
            using var stream = _client.GetStream();

            while (!token.IsCancellationRequested && _client.Connected)
            {
                // Receive MessagePack commands from server
                var command = await ReceiveMessagePackAsync(stream, token);
                if (command == null)
                {
                    Logger.LogWrite("Connection lost or received invalid command.");
                    break; // trigger reconnect
                }

                Logger.LogWrite($"Command received: {command.Values?.Count ?? 0} command(s)");

                // Handle reconnect command specially - don't break connection
                bool hasReconnectCommand = command.Values?.Any(cmd => cmd.Type == Shared.Enums.ECommandType.Reconnect) == true;

                if (hasReconnectCommand)
                {
                    Logger.LogWrite("Received reconnect command - will rejoin game server");
                }

                // Always invoke the event for command processing
                OnMessageReceived?.Invoke(this, command);

                // For reconnect commands, we DON'T break the TCP connection
                // The bot will handle rejoining the game server while keeping TCP alive
            }
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            Logger.LogWrite($"Listen error: {ex.Message}");
        }
        finally
        {
            _keepAliveTimer.Stop();

            try { _client?.Close(); } catch { }
            _client = null;

            // Only reconnect if we haven't been cancelled
            if (!token.IsCancellationRequested)
            {
                await ReconnectLoopAsync(token);
            }
        }
    }

    private async Task ReconnectLoopAsync(CancellationToken token)
    {
        Logger.LogWrite("TCP connection lost - attempting to reconnect...");

        while (!token.IsCancellationRequested)
        {
            try
            {
                await ConnectAsync(token);
                Logger.LogWrite("TCP reconnected successfully.");
                return;
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                Logger.LogWrite($"TCP reconnect failed: {ex.Message}. Retrying in 10s...");
                await Task.Delay(TimeSpan.FromSeconds(10), token);
            }
        }
    }

    public async Task<BotCommand?> ReceiveMessagePackAsync(NetworkStream stream, CancellationToken token)
    {
        try
        {
            var lengthBuffer = new byte[4];
            int totalRead = 0;

            // Ensure we read exactly 4 bytes for length
            while (totalRead < 4)
            {
                int read = await stream.ReadAsync(lengthBuffer, totalRead, 4 - totalRead, token);
                if (read == 0) return null; // Connection closed
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

            // Read the message data
            var data = new byte[length];
            int offset = 0;
            while (offset < length)
            {
                int chunk = await stream.ReadAsync(data, offset, length - offset, token);
                if (chunk == 0) return null; // Connection closed
                offset += chunk;
            }

            return MessagePackSerializer.Deserialize<BotCommand>(data);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogWrite($"Receive error: {ex.Message}");
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
        _keepAliveTimer.Stop();

        try
        {
            _client?.Close();
            _client?.Dispose();
        }
        catch { }

        _client = null;
    }
}