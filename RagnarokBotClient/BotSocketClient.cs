// ===== FIXED CLIENT =====
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
    public event EventHandler? OnPinged;

    public BotSocketClient(string host, int port, long serverId)
    {
        _host = host;
        _port = port;
        _serverId = serverId;
        _keepAliveTimer = new();
        _keepAliveTimer.Interval = 120000;
        _keepAliveTimer.Tick += KeepAliveTimer_Tick;
    }

    private async void KeepAliveTimer_Tick(object? sender, EventArgs e)
    {
        if (_client?.Connected == true)
        {
            await SendStringMessageAsync(_client.GetStream(), $"{_serverId}:{_botId}");
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

                Logger.LogWrite("Connected to server");

                // send bot ID immediately (handshake) - use string message
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
                // Try to receive MessagePack data (commands from server)
                var command = await ReceiveMessagePackAsync(stream, token);
                if (command == null)
                {
                    Logger.LogWrite("Received null command or disconnected from server.");
                    break; // trigger reconnect
                }

                // Check if it's a reconnect command
                if (command.Values != null && command.Values.Any(cmd => cmd.Type == Shared.Enums.ECommandType.Reconnect))
                {
                    Logger.LogWrite("Received reconnect command from server.");
                    OnMessageReceived?.Invoke(this, command);
                    break; // trigger reconnect
                }

                // Send ACK as string message
                await SendStringMessageAsync(stream, $"ACK:{_serverId}:{_botId}", token);

                Logger.LogWrite("Command received and ACK sent.");
                OnMessageReceived?.Invoke(this, command);
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

            await ReconnectLoopAsync(token);
        }
    }

    private async Task ReconnectLoopAsync(CancellationToken token)
    {
        if (!token.IsCancellationRequested) Logger.LogWrite("Attempting to reconnect...");

        while (!token.IsCancellationRequested)
        {
            try
            {
                await ConnectAsync(token);
                Logger.LogWrite("Reconnected successfully.");
                return;
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                Logger.LogWrite($"Reconnect failed: {ex.Message}. Retrying in 10s...");
                await Task.Delay(TimeSpan.FromSeconds(10), token);
            }
        }
    }

    // Receive MessagePack serialized BotCommand
    public async Task<BotCommand?> ReceiveMessagePackAsync(NetworkStream stream, CancellationToken token)
    {
        try
        {
            var lengthBuffer = new byte[4];
            int read = await stream.ReadAsync(lengthBuffer, 0, 4, token);
            if (read == 0) return null;

            // Server sends big-endian length, so reverse if we're little-endian
            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBuffer);

            int length = BitConverter.ToInt32(lengthBuffer, 0);
            if (length <= 0 || length > 1024 * 1024) // Add reasonable size limit
            {
                Logger.LogWrite($"Invalid message length: {length}");
                return null;
            }

            var data = new byte[length];
            int offset = 0;
            while (offset < length)
            {
                int chunk = await stream.ReadAsync(data, offset, length - offset, token);
                if (chunk == 0) return null;
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
            Logger.LogWrite($"Receive MessagePack error: {ex.Message}");
            return null;
        }
    }

    // Send string message (for handshake and ACK)
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
            Logger.LogWrite($"Send string message error: {ex.Message}");
            throw;
        }
    }
}
