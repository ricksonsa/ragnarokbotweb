using FluentFTP;
using RagnarokBotWeb.Domain.Entities;
using System.Collections.Concurrent;

namespace RagnarokBotWeb.Infrastructure.FTP;

public class FtpConnectionPool : IDisposable
{
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _expirationTime;
    private readonly ILogger<FtpConnectionPool> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentBag<(FtpClient Client, DateTime CreatedAt)>> _pools = new();

    public FtpConnectionPool(ILogger<FtpConnectionPool> logger)
    {
        _logger = logger;
        _expirationTime = TimeSpan.FromMinutes(2);
        _cleanupTimer = new Timer(RemoveExpiredConnections, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    public void Dispose()
    {
        try
        {
            _cleanupTimer.Dispose();
            foreach (var (key, bag) in _pools)
            {
                bag.TryTake(out var expiredConnection);
                {
                    expiredConnection.Client.Disconnect();
                    expiredConnection.Client.Dispose();
                }
            }

            GC.SuppressFinalize(this);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while cleaning up FTP connections.");
        }
    }

    public FtpClient GetClient(Ftp ftp)
    {
        var key = GetKey(ftp);

        if (_pools.TryGetValue(key, out var bag))
            while (bag.TryPeek(out var item))
            {
                if (!IsExpired(item.CreatedAt))
                {
                    if (!item.Client.IsConnected) item.Client.Connect();
                    return item.Client;
                }

                RemoveExpiredConnections(key, bag);
            }

        var client = FtpClientFactory.CreateClient(ftp);

        _pools.TryAdd(key, [(client, DateTime.UtcNow)]);

        return client;
    }

    private void RemoveExpiredConnections(object? state)
    {
        foreach (var key in _pools.Keys)
        {
            if (!_pools.TryGetValue(key, out var bag)) continue;

            foreach (var (client, createdAt) in bag)
                if (IsExpired(createdAt))
                    RemoveExpiredConnections(key, bag);
        }
    }

    private bool IsExpired(DateTime createdAt)
    {
        return DateTime.UtcNow - createdAt > _expirationTime;
    }

    private void RemoveExpiredConnections(string key, ConcurrentBag<(FtpClient Client, DateTime CreatedAt)> bag)
    {
        try
        {
            if (bag.TryTake(out var expiredConnection))
            {
                expiredConnection.Client?.Disconnect();
                expiredConnection.Client?.Dispose();
                _logger.LogInformation("Expired FTP connection for key '{}' removed.", key);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while disconnecting ftp client.");
        }
    }

    private static string GetKey(Ftp ftp)
    {
        return ftp.UserName + ftp.Address;
    }
}