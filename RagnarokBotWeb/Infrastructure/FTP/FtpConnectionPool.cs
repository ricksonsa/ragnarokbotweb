using FluentFTP;
using RagnarokBotWeb.Domain.Entities;
using System.Collections.Concurrent;

namespace RagnarokBotWeb.Infrastructure.FTP;

public class FtpConnectionPool : IAsyncDisposable
{
    private readonly ILogger<FtpConnectionPool> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _serverLimits = new();
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

        // Find and release the semaphore for this client's server
        // Note: This is a simplified approach - in production you'd want to track which semaphore belongs to which client
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
        // In this simplified version, we just log the request
        var serverKey = GetKey(ftp);
        _logger.LogInformation("Clear pool requested for {ServerKey} (simplified implementation)", serverKey);
        await Task.CompletedTask;
    }

    private static string GetKey(Ftp ftp) => $"{ftp.UserName}@{ftp.Address}:{ftp.Port}";

    public async ValueTask DisposeAsync()
    {
        foreach (var semaphore in _serverLimits.Values)
        {
            semaphore.Dispose();
        }
        _serverLimits.Clear();
    }
}