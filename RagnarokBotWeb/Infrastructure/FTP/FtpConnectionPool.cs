using FluentFTP;
using RagnarokBotWeb.Domain.Entities;
using System.Collections.Concurrent;

namespace RagnarokBotWeb.Infrastructure.FTP;

public class FtpConnectionPool : IAsyncDisposable
{
    private class PooledClient
    {
        public AsyncFtpClient Client { get; }
        public DateTime CreatedAt { get; }
        public bool InUse { get; set; }

        public PooledClient(AsyncFtpClient client)
        {
            Client = client;
            CreatedAt = DateTime.UtcNow;
        }
    }

    private readonly ILogger<FtpConnectionPool> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentBag<PooledClient>> _pools = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly CancellationTokenSource _cts = new();

    public FtpConnectionPool(ILogger<FtpConnectionPool> logger)
    {
        _logger = logger;
        _ = Task.Run(CleanupLoopAsync);
    }

    public async Task<AsyncFtpClient> GetClientAsync(Ftp ftp, CancellationToken cancellationToken = default)
    {
        var key = GetKey(ftp);
        var bag = _pools.GetOrAdd(key, _ => new ConcurrentBag<PooledClient>());
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        // Try reuse
        foreach (var pooledClient in bag)
        {
            if (!pooledClient.InUse && !IsExpired(pooledClient.CreatedAt))
            {
                lock (pooledClient)
                {
                    if (pooledClient.InUse) continue;
                    pooledClient.InUse = true;
                }

                try
                {
                    if (!pooledClient.Client.IsConnected)
                        await pooledClient.Client.Connect(cancellationToken);

                    return pooledClient.Client;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to reuse FTP client, will remove.");
                    await DisposeClientAsync(pooledClient.Client);
                    bag.TryTake(out _);
                    break;
                }
            }
        }

        // No reusable client, create new
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check after lock
            foreach (var pooledClient in bag)
            {
                if (!pooledClient.InUse && !IsExpired(pooledClient.CreatedAt))
                {
                    lock (pooledClient)
                    {
                        if (pooledClient.InUse) continue;
                        pooledClient.InUse = true;
                    }

                    if (!pooledClient.Client.IsConnected)
                        await pooledClient.Client.Connect(cancellationToken);

                    return pooledClient.Client;
                }
            }

            var newClient = FtpClientFactory.CreateClient(ftp, cancellationToken: cancellationToken);
            await newClient.Connect(cancellationToken);

            var pooled = new PooledClient(newClient) { InUse = true };
            bag.Add(pooled);
            return newClient;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public Task ReleaseClientAsync(AsyncFtpClient client)
    {
        foreach (var (_, bag) in _pools)
        {
            foreach (var pooledClient in bag)
            {
                if (ReferenceEquals(pooledClient.Client, client))
                {
                    lock (pooledClient)
                    {
                        if (!pooledClient.InUse)
                            throw new InvalidOperationException("Client already released.");
                        pooledClient.InUse = false;
                    }
                    return Task.CompletedTask;
                }
            }
        }

        // Not in pool, dispose
        return DisposeClientAsync(client);
    }

    private async Task DisposeClientAsync(AsyncFtpClient client)
    {
        try
        {
            if (client.IsConnected)
                await client.Disconnect();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting FTP client.");
        }
        finally
        {
            client.Dispose();
        }
    }

    private async Task CleanupLoopAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                foreach (var (key, bag) in _pools)
                {
                    var keep = new List<PooledClient>();
                    while (bag.TryTake(out var pooled))
                    {
                        if (IsExpired(pooled.CreatedAt) && !pooled.InUse)
                        {
                            await DisposeClientAsync(pooled.Client);
                            _logger.LogInformation("Removed expired FTP client for {Key}", key);
                        }
                        else
                        {
                            keep.Add(pooled);
                        }
                    }
                    foreach (var item in keep) bag.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cleanup loop.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), _cts.Token);
        }
    }

    private static string GetKey(Ftp ftp) => $"{ftp.UserName}@{ftp.Address}";
    private static bool IsExpired(DateTime createdAt) => DateTime.UtcNow - createdAt > TimeSpan.FromMinutes(2);

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        foreach (var (_, bag) in _pools)
        {
            while (bag.TryTake(out var pooled))
                await DisposeClientAsync(pooled.Client);
        }
        _cts.Dispose();
    }
}
