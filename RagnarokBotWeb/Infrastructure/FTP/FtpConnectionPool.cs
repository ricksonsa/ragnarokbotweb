using FluentFTP;
using RagnarokBotWeb.Domain.Entities;
using System.Collections.Concurrent;

namespace RagnarokBotWeb.Infrastructure.FTP;

public class FtpConnectionPool : IAsyncDisposable
{
    private readonly ILogger<FtpConnectionPool> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _serverLimits = new();
    private readonly ConcurrentDictionary<AsyncFtpClient, string> _clientToServerMap = new();
    private const int MaxConnectionsPerServer = 3;

    public FtpConnectionPool(ILogger<FtpConnectionPool> logger)
    {
        _logger = logger;
    }

    public async Task<AsyncFtpClient> GetClientAsync(Ftp ftp, CancellationToken cancellationToken = default)
    {
        var serverKey = GetKey(ftp);
        var semaphore = _serverLimits.GetOrAdd(serverKey, _ => new SemaphoreSlim(MaxConnectionsPerServer, MaxConnectionsPerServer));

        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var client = FtpClientFactory.CreateClient(ftp, cancellationToken: cancellationToken);
            await client.Connect(cancellationToken);

            // Track which server this client belongs to
            _clientToServerMap.TryAdd(client, serverKey);

            _logger.LogDebug("Created new FTP client for {ServerKey}", serverKey);
            return client;
        }
        catch (Exception ex)
        {
            semaphore.Release(); // Release semaphore if client creation fails
            _logger.LogError(ex, "Failed to create FTP client for {ServerKey}", serverKey);
            throw;
        }
    }

    public async Task ReleaseClientAsync(AsyncFtpClient client)
    {
        if (client == null) return;

        // Find and release the correct semaphore for this client's server
        if (_clientToServerMap.TryRemove(client, out var serverKey))
        {
            if (_serverLimits.TryGetValue(serverKey, out var semaphore))
            {
                semaphore.Release();
                _logger.LogDebug("Released semaphore for server {ServerKey}", serverKey);
            }
        }

        try
        {
            if (client.IsConnected && !client.IsDisposed)
            {
                await client.Disconnect();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error disconnecting FTP client during release");
        }
        finally
        {
            try
            {
                if (!client.IsDisposed)
                {
                    client.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error disposing FTP client during release");
            }
        }
    }

    public async Task ClearPoolAsync(Ftp ftp)
    {
        var serverKey = GetKey(ftp);

        // Find and dispose all clients for this specific server
        var clientsToRemove = _clientToServerMap
            .Where(kvp => kvp.Value == serverKey)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var client in clientsToRemove)
        {
            await ReleaseClientAsync(client);
        }

        _logger.LogInformation("Cleared pool for {ServerKey}, removed {Count} clients", serverKey, clientsToRemove.Count);
    }

    private static string GetKey(Ftp ftp) => $"{ftp.UserName}@{ftp.Address}:{ftp.Port}:{ftp.RootFolder ?? ""}";

    public async ValueTask DisposeAsync()
    {
        // Dispose all clients first
        var allClients = _clientToServerMap.Keys.ToList();
        foreach (var client in allClients)
        {
            await ReleaseClientAsync(client);
        }

        // Then dispose semaphores
        foreach (var semaphore in _serverLimits.Values)
        {
            semaphore.Dispose();
        }

        _serverLimits.Clear();
        _clientToServerMap.Clear();
    }
}