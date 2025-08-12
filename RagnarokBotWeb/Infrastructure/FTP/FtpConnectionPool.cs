using FluentFTP;
using RagnarokBotWeb.Domain.Entities;
using System.Collections.Concurrent;

namespace RagnarokBotWeb.Infrastructure.FTP;

public class FtpConnectionPool : IDisposable
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
            InUse = false;
        }
    }

    private readonly Timer _cleanupTimer;
    private readonly ILogger<FtpConnectionPool> _logger;

    // Pools keyed by ftp info: key -> list of pooled clients
    private readonly ConcurrentDictionary<string, ConcurrentBag<PooledClient>> _pools = new();

    // Locks per key to avoid concurrent client creation
    private readonly ConcurrentDictionary<string, object> _locks = new();

    public FtpConnectionPool(ILogger<FtpConnectionPool> logger)
    {
        _logger = logger;
        _cleanupTimer = new Timer(RemoveExpiredConnections, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    public AsyncFtpClient GetClient(Ftp ftp, CancellationToken cancellationToken = default)
    {
        var key = GetKey(ftp);
        var bag = _pools.GetOrAdd(key, _ => new ConcurrentBag<PooledClient>());
        var lockObj = _locks.GetOrAdd(key, _ => new object());

        // Try to find an available client
        foreach (var pooledClient in bag)
        {
            if (!pooledClient.InUse && !IsExpired(pooledClient.CreatedAt))
            {
                // Mark as in use and check connection
                if (TryLeaseClient(pooledClient))
                {
                    try
                    {
                        if (!pooledClient.Client.IsConnected)
                            pooledClient.Client.Connect();

                        return pooledClient.Client;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to connect reused FTP client.");
                        DisposeClient(pooledClient.Client);
                        bag.TryTake(out var _); // Remove bad client
                        break; // will create new client
                    }
                }
            }
            else if (IsExpired(pooledClient.CreatedAt) && !pooledClient.InUse)
            {
                if (bag.TryTake(out var expiredClient))
                {
                    DisposeClient(expiredClient.Client);
                    _logger.LogInformation("Expired FTP connection for key '{Key}' removed during GetClient.", key);
                }
            }
        }

        // No reusable client available, create new one (locked per key)
        lock (lockObj)
        {
            // Double-check inside lock (in case other thread added client)
            foreach (var pooledClient in bag)
            {
                if (!pooledClient.InUse && !IsExpired(pooledClient.CreatedAt))
                {
                    if (TryLeaseClient(pooledClient))
                    {
                        try
                        {
                            if (!pooledClient.Client.IsConnected)
                                pooledClient.Client.Connect();

                            return pooledClient.Client;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to connect reused FTP client.");
                            DisposeClient(pooledClient.Client);
                            bag.TryTake(out var _);
                            break;
                        }
                    }
                }
            }

            // Create new client
            var newClient = FtpClientFactory.CreateClient(ftp, cancellationToken: cancellationToken);
            try
            {
                newClient.Connect();
                var pooled = new PooledClient(newClient) { InUse = true };
                bag.Add(pooled);
                return newClient;
            }
            catch (Exception ex)
            {
                newClient.Dispose();
                _logger.LogError(ex, "Failed to connect new FTP client. Ftp Address[{Address}]", ftp.Address);
                throw;
            }
        }
    }

    /// <summary>
    /// Release a leased client back to the pool for reuse.
    /// Caller MUST call this after done using the client.
    /// </summary>
    public void ReleaseClient(AsyncFtpClient client)
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

                        // Optional: check client health and dispose if disconnected
                        if (!pooledClient.Client.IsConnected)
                        {
                            DisposeClient(pooledClient.Client);
                            bag.TryTake(out var _); // Remove from pool
                        }
                    }
                    return;
                }
            }
        }

        // If client not found in pool, dispose it to be safe
        DisposeClient(client);
    }


    private bool TryLeaseClient(PooledClient pooledClient)
    {
        // Atomically check and set InUse to true if it was false
        lock (pooledClient)
        {
            if (pooledClient.InUse)
                return false;
            pooledClient.InUse = true;
            return true;
        }
    }

    private void RemoveExpiredConnections(object? state)
    {
        foreach (var (key, bag) in _pools)
        {
            var validClients = new List<PooledClient>();

            while (bag.TryTake(out var item))
            {
                if (IsExpired(item.CreatedAt) && !item.InUse)
                {
                    DisposeClient(item.Client);
                    _logger.LogInformation("Expired FTP connection for key '{Key}' removed.", key);
                }
                else
                {
                    validClients.Add(item);
                }
            }

            foreach (var client in validClients)
                bag.Add(client);
        }
    }

    private void DisposeClient(AsyncFtpClient client)
    {
        try
        {
            if (client.IsConnected)
                client.Disconnect();

            client.Dispose();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while disconnecting FTP client.");
        }
    }

    private static bool IsExpired(DateTime createdAt) =>
        DateTime.UtcNow - createdAt > TimeSpan.FromMinutes(2);

    private static string GetKey(Ftp ftp) =>
        $"{ftp.UserName}@{ftp.Address}";

    public void Dispose()
    {
        _cleanupTimer.Dispose();

        foreach (var (_, bag) in _pools)
        {
            while (bag.TryTake(out var item))
            {
                DisposeClient(item.Client);
            }
        }

        GC.SuppressFinalize(this);
    }
}
