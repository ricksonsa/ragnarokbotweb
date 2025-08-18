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
    private string _botId;
    private readonly long _serverId;

    private System.Windows.Forms.Timer _keepAliveTimer;

    public event EventHandler<BotCommand> OnMessageReceived;
    public event EventHandler OnPinged;

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
        await SendMessageAsync($"{_serverId}:{_botId}");
    }

    public async Task ConnectAsync(string botId, CancellationToken token = default)
    {
        _botId = botId;

        while (!token.IsCancellationRequested)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_host, _port, token);

                Logger.LogWrite("Connected to server");

                // send bot ID immediately
                await SendMessageAsync($"{_serverId}:{_botId}");

                _ = ListenAsync(token); // fire-and-forget listen loop
                return; // exit connect loop on success
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                Logger.LogWrite($"Connection failed: {ex.Message}. Retrying in 5s...");
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

            while (!token.IsCancellationRequested)
            {
                var command = await ReceiveMessage(stream, token);
                if (command == null)
                {
                    Logger.LogWrite("Disconnected from server.");
                    break; // trigger reconnect
                }

                Logger.LogWrite("Command received.");
                await SendMessageAsync(stream, $"ACK:{_botId}", token);
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

            // Dispose old client
            try { _client?.Close(); } catch { }
            _client = null;

            // Try reconnect
            await ReconnectLoopAsync(token);
        }
    }

    private async Task ReconnectLoopAsync(CancellationToken token)
    {
        Logger.LogWrite("Attempting to reconnect...");

        while (!token.IsCancellationRequested)
        {
            try
            {
                await ConnectAsync(_botId, token);
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

    public async Task HandleServerMessage(NetworkStream stream, CancellationToken token)
    {
        var command = await ReceiveMessage(stream, token);
        if (command != null)
        {
            Logger.LogWrite($"Command received.");

            // send ACK back
            var now = DateTime.UtcNow;
            await SendMessageAsync(stream, $"ACK:{_botId}");
            OnMessageReceived?.Invoke(this, command);
        }
    }

    public async Task<BotCommand?> ReceiveMessage(NetworkStream stream, CancellationToken token)
    {
        try
        {
            // Read length prefix (4 bytes)
            var lengthBuffer = new byte[4];
            int read = await stream.ReadAsync(lengthBuffer, 0, 4, token);
            if (read == 0) return null; // disconnected

            int length = BitConverter.ToInt32(lengthBuffer, 0);
            if (length <= 0) return null;

            // Read full message
            var data = new byte[length];
            int offset = 0;
            while (offset < length)
            {
                int chunk = await stream.ReadAsync(data, offset, length - offset, token);
                if (chunk == 0) return null; // disconnected mid-message
                offset += chunk;
            }

            // Deserialize using MessagePack
            return MessagePackSerializer.Deserialize<BotCommand>(data);
        }
        catch (OperationCanceledException)
        {
            return null; // clean cancellation
        }
        catch (Exception ex)
        {
            // optionally log ex
            return null;
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (_client == null || !_client.Connected) return;

        var bytes = Encoding.UTF8.GetBytes(message);
        await _client.GetStream().WriteAsync(bytes, 0, bytes.Length);
    }

    public async Task SendMessageAsync(NetworkStream stream, string message, CancellationToken token = default)
    {
        // Encode the message into bytes
        byte[] data = Encoding.UTF8.GetBytes(message);

        // Prefix with the length (4 bytes, big endian)
        byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthPrefix); // ensure network order

        // Send length + data
        await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length, token);
        await stream.WriteAsync(data, 0, data.Length, token);
        await stream.FlushAsync(token);
    }

}
