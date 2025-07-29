using FluentFTP;
using RagnarokBotWeb.Domain.Entities;
using System.Collections.Concurrent;

namespace RagnarokBotWeb.Infrastructure.FTP;

public class FtpConnectionPool : IDisposable
{
    private readonly Timer _cleanupTimer;
    private readonly ILogger<FtpConnectionPool> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentBag<(FtpClient Client, DateTime CreatedAt)>> _pools = new();

    public FtpConnectionPool(ILogger<FtpConnectionPool> logger)
    {
        _logger = logger;
        _cleanupTimer = new Timer(RemoveExpiredConnections, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    public FtpClient GetClient(Ftp ftp)
    {
        var key = GetKey(ftp);
        var bag = _pools.GetOrAdd(key, _ => new ConcurrentBag<(FtpClient, DateTime)>());

        while (bag.TryTake(out var item))
        {
            if (!IsExpired(item.CreatedAt))
            {
                try
                {
                    if (!item.Client.IsConnected)
                        item.Client.Connect();

                    return item.Client;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to connect reused FTP client.");
                    item.Client.Dispose();
                }
            }
            else
            {
                DisposeClient(item.Client);
                _logger.LogInformation("Expired FTP connection for key '{Key}' removed during GetClient.", key);
            }
        }

        var newClient = FtpClientFactory.CreateClient(ftp);
        try
        {
            newClient.Connect();
            bag.Add((newClient, DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            newClient.Dispose();
            _logger.LogError(ex, "Failed to connect new FTP client. Ftp Address[{Address}]", ftp.Address);
            throw;
        }

        return newClient;
    }

    private void RemoveExpiredConnections(object? state)
    {
        foreach (var (key, bag) in _pools)
        {
            var validConnections = new ConcurrentBag<(FtpClient Client, DateTime CreatedAt)>();

            while (bag.TryTake(out var item))
            {
                if (IsExpired(item.CreatedAt))
                {
                    DisposeClient(item.Client);
                    _logger.LogInformation("Expired FTP connection for key '{Key}' removed.", key);
                }
                else
                {
                    validConnections.Add(item);
                }
            }

            foreach (var item in validConnections)
                bag.Add(item);
        }
    }

    private void DisposeClient(FtpClient client)
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
