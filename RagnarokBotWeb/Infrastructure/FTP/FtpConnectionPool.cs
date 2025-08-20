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
        public DateTime LastUsed { get; set; }
        public bool InUse { get; set; }
        public bool IsHealthy { get; set; } = true;
        public string ClientId { get; } = Guid.NewGuid().ToString();

        public PooledClient(AsyncFtpClient client)
        {
            Client = client;
            CreatedAt = DateTime.UtcNow;
            LastUsed = DateTime.UtcNow;
        }
    }

    private readonly ILogger<FtpConnectionPool> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentBag<PooledClient>> _pools = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ConcurrentDictionary<AsyncFtpClient, PooledClient> _clientToPooled = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly TimeSpan _maxAge = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _maxIdleTime = TimeSpan.FromMinutes(2);
    private readonly object _poolLock = new object();

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

        // Try to reuse an existing healthy client
        var reusableClient = await TryReuseClientAsync(bag, key, cancellationToken);
        if (reusableClient != null)
            return reusableClient;

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            reusableClient = await TryReuseClientAsync(bag, key, cancellationToken);
            if (reusableClient != null)
                return reusableClient;

            // Create new client
            var newClient = FtpClientFactory.CreateClient(ftp, cancellationToken: cancellationToken);
            await newClient.Connect(cancellationToken);

            var pooled = new PooledClient(newClient) { InUse = true };

            lock (_poolLock)
            {
                bag.Add(pooled);
                _clientToPooled[newClient] = pooled;
            }

            _logger.LogDebug("Created new FTP client {ClientId} for {Key}", pooled.ClientId, key);
            return newClient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create FTP client for {Key}", key);
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<AsyncFtpClient?> TryReuseClientAsync(ConcurrentBag<PooledClient> bag, string key, CancellationToken cancellationToken)
    {
        var clientsToRemove = new List<PooledClient>();
        var clientsToReAdd = new List<PooledClient>();

        // Collect all clients from the bag
        var allClients = new List<PooledClient>();
        while (bag.TryTake(out var client))
        {
            allClients.Add(client);
        }

        PooledClient? selectedClient = null;

        foreach (var pooledClient in allClients)
        {
            // Skip if already selected a client
            if (selectedClient != null)
            {
                clientsToReAdd.Add(pooledClient);
                continue;
            }

            // Check if client should be removed
            if (IsExpired(pooledClient) || !pooledClient.IsHealthy)
            {
                clientsToRemove.Add(pooledClient);
                continue;
            }

            // Try to acquire this client
            bool acquired = false;
            lock (_poolLock)
            {
                if (!pooledClient.InUse)
                {
                    pooledClient.InUse = true;
                    acquired = true;
                }
            }

            if (acquired)
            {
                try
                {
                    // Validate connection health
                    if (await IsClientHealthyAsync(pooledClient.Client, cancellationToken))
                    {
                        pooledClient.LastUsed = DateTime.UtcNow;
                        pooledClient.IsHealthy = true;
                        selectedClient = pooledClient;
                        //_logger.LogDebug("Reusing FTP client {ClientId} for {Key}", pooledClient.ClientId, key);
                    }
                    else
                    {
                        _logger.LogInformation("FTP client {ClientId} failed health check, marking for removal", pooledClient.ClientId);
                        pooledClient.IsHealthy = false;
                        clientsToRemove.Add(pooledClient);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to validate FTP client {ClientId} health, marking for removal", pooledClient.ClientId);
                    pooledClient.IsHealthy = false;
                    clientsToRemove.Add(pooledClient);
                }
                finally
                {
                    if (selectedClient == null)
                    {
                        lock (_poolLock)
                        {
                            pooledClient.InUse = false;
                        }
                    }
                }
            }
            else
            {
                // Client is in use, put it back
                clientsToReAdd.Add(pooledClient);
            }
        }

        // Re-add clients that should stay in the pool
        foreach (var client in clientsToReAdd)
        {
            bag.Add(client);
        }

        if (selectedClient != null)
        {
            bag.Add(selectedClient);
        }

        // Remove and dispose unhealthy clients
        foreach (var client in clientsToRemove)
        {
            lock (_poolLock)
            {
                _clientToPooled.TryRemove(client.Client, out _);
            }
            _ = Task.Run(() => DisposeClientAsync(client.Client));
        }

        return selectedClient?.Client;
    }

    private async Task<bool> IsClientHealthyAsync(AsyncFtpClient client, CancellationToken cancellationToken)
    {
        try
        {
            if (!client.IsConnected)
            {
                await client.Connect(cancellationToken);
            }

            // Perform a lightweight operation to test connectivity
            await client.GetWorkingDirectory(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Client health check failed");
            return false;
        }
    }

    public Task ReleaseClientAsync(AsyncFtpClient client)
    {
        if (client == null)
        {
            _logger.LogWarning("Attempting to release null client");
            return Task.CompletedTask;
        }

        bool found = false;
        string? clientId = null;

        lock (_poolLock)
        {
            if (_clientToPooled.TryGetValue(client, out var pooledClient))
            {
                clientId = pooledClient.ClientId;
                if (pooledClient.InUse)
                {
                    pooledClient.InUse = false;
                    pooledClient.LastUsed = DateTime.UtcNow;
                    found = true;
                    //_logger.LogDebug("Released FTP client {ClientId}", clientId);
                }
                else
                {
                    _logger.LogWarning("Attempting to release FTP client {ClientId} that's not marked as in use", clientId);
                }
            }
        }

        if (!found)
        {
            // Client not found in pool, dispose it directly
            _logger.LogDebug("FTP client not found in pool, disposing directly");
            return DisposeClientAsync(client);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all cached connections for a specific FTP server (useful when server goes offline)
    /// </summary>
    public async Task ClearPoolAsync(Ftp ftp)
    {
        var key = GetKey(ftp);

        if (_pools.TryRemove(key, out var bag))
        {
            var disposeTasks = new List<Task>();
            var clientsToRemove = new List<PooledClient>();

            // Collect all clients
            while (bag.TryTake(out var pooled))
            {
                clientsToRemove.Add(pooled);
            }

            // Remove from tracking dictionary and dispose
            lock (_poolLock)
            {
                foreach (var pooled in clientsToRemove)
                {
                    _clientToPooled.TryRemove(pooled.Client, out _);
                    disposeTasks.Add(DisposeClientAsync(pooled.Client));
                }
            }

            await Task.WhenAll(disposeTasks);
            _logger.LogInformation("Cleared connection pool for {Key}, disposed {Count} clients", key, clientsToRemove.Count);
        }
        else
        {
            _logger.LogDebug("No connection pool found for {Key}", key);
        }
    }

    /// <summary>
    /// Clears all cached connections (useful for global cleanup)
    /// </summary>
    public async Task ClearAllPoolsAsync()
    {
        var disposeTasks = new List<Task>();
        var totalClients = 0;

        foreach (var (key, bag) in _pools.ToArray())
        {
            var clientsInThisPool = 0;
            while (bag.TryTake(out var pooled))
            {
                disposeTasks.Add(DisposeClientAsync(pooled.Client));
                clientsInThisPool++;
                totalClients++;
            }
            _logger.LogDebug("Clearing {Count} clients from pool {Key}", clientsInThisPool, key);
        }

        lock (_poolLock)
        {
            _clientToPooled.Clear();
        }
        _pools.Clear();

        if (disposeTasks.Count > 0)
        {
            await Task.WhenAll(disposeTasks);
        }

        _logger.LogInformation("Cleared all connection pools, disposed {Count} clients", totalClients);
    }

    private async Task DisposeClientAsync(AsyncFtpClient client)
    {
        if (client == null) return;

        try
        {
            if (client.IsConnected)
            {
                await client.Disconnect();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disconnecting FTP client during disposal");
        }
        finally
        {
            try
            {
                client.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing FTP client");
            }
        }
    }

    private async Task CleanupLoopAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync();
                await Task.Delay(TimeSpan.FromMinutes(1), _cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cleanup loop");
                await Task.Delay(TimeSpan.FromSeconds(30), _cts.Token);
            }
        }
    }

    private async Task PerformCleanupAsync()
    {
        var cleanupTasks = new List<Task>();
        var totalCleaned = 0;

        foreach (var (key, bag) in _pools.ToArray())
        {
            var itemsToKeep = new List<PooledClient>();
            var itemsToDispose = new List<PooledClient>();

            // Collect items to process
            while (bag.TryTake(out var pooled))
            {
                bool shouldDispose = false;

                lock (_poolLock)
                {
                    if (!pooled.InUse)
                    {
                        if (IsExpired(pooled) || IsIdleExpired(pooled) || !pooled.IsHealthy)
                        {
                            shouldDispose = true;
                            _clientToPooled.TryRemove(pooled.Client, out _);
                        }
                    }
                }

                if (shouldDispose)
                {
                    itemsToDispose.Add(pooled);
                }
                else
                {
                    itemsToKeep.Add(pooled);
                }
            }

            // Add back items to keep
            foreach (var item in itemsToKeep)
            {
                bag.Add(item);
            }

            // Dispose expired/unhealthy items
            foreach (var item in itemsToDispose)
            {
                cleanupTasks.Add(DisposeClientAsync(item.Client));
            }

            if (itemsToDispose.Count > 0)
            {
                totalCleaned += itemsToDispose.Count;
                _logger.LogDebug("Cleaned up {Count} expired FTP clients for {Key}", itemsToDispose.Count, key);
            }
        }

        if (cleanupTasks.Count > 0)
        {
            await Task.WhenAll(cleanupTasks);
            _logger.LogDebug("Cleanup completed, disposed {Count} clients total", totalCleaned);
        }
    }

    private static string GetKey(Ftp ftp) => $"{ftp.UserName}@{ftp.Address}:{ftp.Port}";

    private bool IsExpired(PooledClient pooled) =>
        DateTime.UtcNow - pooled.CreatedAt > _maxAge;

    private bool IsIdleExpired(PooledClient pooled) =>
        DateTime.UtcNow - pooled.LastUsed > _maxIdleTime;

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        await ClearAllPoolsAsync();

        foreach (var semaphore in _locks.Values)
        {
            semaphore.Dispose();
        }

        _cts.Dispose();
    }
}