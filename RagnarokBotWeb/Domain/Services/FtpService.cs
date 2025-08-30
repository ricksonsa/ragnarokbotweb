using FluentFTP;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.FTP;
using System.Collections.Concurrent;
using System.Text;

namespace RagnarokBotWeb.Domain.Services;

public class FtpService : IFtpService, IAsyncDisposable
{
    private readonly ILogger<FtpService> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _serverSemaphores = new();
    private readonly ConcurrentDictionary<string, int> _activeConnections = new();
    private const int MaxConnectionsPerServer = 3;

    public FtpService(ILogger<FtpService> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Ftp ftp, Func<AsyncFtpClient, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var serverKey = GetServerKey(ftp);
        var semaphore = _serverSemaphores.GetOrAdd(serverKey, _ => new SemaphoreSlim(MaxConnectionsPerServer, MaxConnectionsPerServer));

        await semaphore.WaitAsync(cancellationToken);

        try
        {
            _activeConnections.AddOrUpdate(serverKey, 1, (key, count) => count + 1);
            _logger.LogDebug("Acquired FTP connection for {ServerKey}, active connections: {Count}",
                serverKey, _activeConnections[serverKey]);

            using var client = await CreateAndConnectClientAsync(ftp, cancellationToken);
            return await operation(client);
        }
        finally
        {
            _activeConnections.AddOrUpdate(serverKey, 0, (key, count) => Math.Max(0, count - 1));
            semaphore.Release();
            _logger.LogDebug("Released FTP connection for {ServerKey}, active connections: {Count}",
                serverKey, _activeConnections.GetValueOrDefault(serverKey, 0));
        }
    }

    private async Task<AsyncFtpClient> CreateAndConnectClientAsync(Ftp ftp, CancellationToken cancellationToken)
    {
        var client = FtpClientFactory.CreateClient(ftp, cancellationToken: cancellationToken);

        try
        {
            await client.Connect(cancellationToken);
            _logger.LogDebug("Created and connected new FTP client for {ServerKey}", GetServerKey(ftp));
            return client;
        }
        catch
        {
            await DisposeClientSafelyAsync(client);
            throw;
        }
    }

    private async Task DisposeClientSafelyAsync(AsyncFtpClient client)
    {
        if (client == null) return;

        try
        {
            if (client.IsConnected && !client.IsDisposed)
            {
                await client.Disconnect();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error disconnecting FTP client during disposal");
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
                _logger.LogDebug(ex, "Error disposing FTP client");
            }
        }
    }

    public async Task<AsyncFtpClient> GetClientAsync(Ftp ftp, CancellationToken cancellationToken = default)
    {
        return await CreateAndConnectClientAsync(ftp, cancellationToken);
    }

    public async Task ReleaseClientAsync(AsyncFtpClient client)
    {
        await DisposeClientSafelyAsync(client);
    }

    public async Task ClearPoolForServerAsync(Ftp ftp)
    {
        var serverKey = GetServerKey(ftp);
        _logger.LogDebug("ClearPoolForServerAsync called for {ServerKey} (no-op in non-pooled implementation)", serverKey);
        await Task.CompletedTask;
    }

    public async Task GetServerConfigLineValue(AsyncFtpClient client, string remoteFilePath, Dictionary<string, string> data)
    {
        try
        {
            if (client.IsDisposed)
                throw new ObjectDisposedException("FTP client was disposed");

            using var stream = await client.OpenRead(remoteFilePath);
            using var reader = new StreamReader(stream, encoding: Encoding.UTF8);

            while (await reader.ReadLineAsync() is { } line)
            {
                foreach (var item in data)
                {
                    if (line.Contains($"scum.{item.Key}="))
                    {
                        data[item.Key] = line.Split("=")[1];
                    }
                }
            }
        }
        catch (ObjectDisposedException)
        {
            _logger.LogError("FTP client was disposed during GetServerConfigLineValue");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetServerConfigLineValue Exception");
            throw;
        }
    }

    public async Task CopyFilesAsync(AsyncFtpClient client, string targetFolder, IList<string> remoteFilePaths, CancellationToken token = default)
    {
        if (client.IsDisposed)
            throw new ObjectDisposedException("FTP client was disposed");

        try
        {
            var results = await client.DownloadFiles(targetFolder, remoteFilePaths, FtpLocalExists.Overwrite, FtpVerify.Throw, token: token);

            if (results.Any(result => result.IsFailed))
            {
                foreach (var result in results.Where(r => r.IsFailed))
                {
                    _logger.LogError(result.Exception, "CopyFilesAsync failed for {RemotePath}", result.RemotePath);
                }
                throw new DomainException("Error while copying files from FTP");
            }
        }
        catch (ObjectDisposedException)
        {
            _logger.LogError("FTP client was disposed during CopyFilesAsync");
            throw;
        }
    }

    public async Task UpdateINILine(AsyncFtpClient client, string remoteFilePath, string key, string newValue)
    {
        try
        {
            if (client.IsDisposed)
                throw new ObjectDisposedException("FTP client was disposed");

            using var stream = new MemoryStream();
            await client.DownloadStream(stream, remoteFilePath);
            stream.Position = 0;

            var content = await new StreamReader(stream).ReadToEndAsync();
            string[] lines = content.Split(Environment.NewLine);
            int lineIndex = Array.FindIndex(lines, line => line.Contains(key));

            if (lineIndex != -1)
            {
                lines[lineIndex] = $"{lines[lineIndex].Split("=")[0]}={newValue}";
                string updatedContent = string.Join(Environment.NewLine, lines);

                using var updatedStream = new MemoryStream(Encoding.UTF8.GetBytes(updatedContent));
                await client.UploadStream(updatedStream, remoteFilePath);
            }
        }
        catch (ObjectDisposedException)
        {
            _logger.LogError("FTP client was disposed during UpdateINILine");
            throw;
        }
        catch (Exception ex)
        {
            throw new DomainException($"Failed to update INI file: {ex.Message}", ex);
        }
    }

    public async Task<Stream?> DownloadFile(AsyncFtpClient client, string remoteFilePath)
    {
        try
        {
            if (client.IsDisposed)
                throw new ObjectDisposedException("FTP client was disposed");

            var stream = new MemoryStream();
            if (await client.DownloadStream(stream, remoteFilePath))
            {
                stream.Position = 0;
                return stream;
            }
            return null;
        }
        catch (ObjectDisposedException)
        {
            _logger.LogError("FTP client was disposed during DownloadFile");
            throw;
        }
        catch (Exception ex)
        {
            throw new DomainException($"Failed to download file: {ex.Message}", ex);
        }
    }

    public async Task RemoveLine(AsyncFtpClient client, string remotePath, string lineToRemove)
    {
        try
        {
            if (client.IsDisposed)
                throw new ObjectDisposedException("FTP client was disposed");

            using var downloadStream = new MemoryStream();
            if (!await client.DownloadStream(downloadStream, remotePath))
                throw new DomainException($"File not found: {remotePath}");

            downloadStream.Position = 0;

            var newLines = new List<string>();
            using (var reader = new StreamReader(downloadStream, Encoding.UTF8, true, 1024, leaveOpen: true))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (!string.Equals(line.Trim(), lineToRemove, StringComparison.Ordinal))
                    {
                        newLines.Add(line);
                    }
                }
            }

            string modifiedContent = string.Join(Environment.NewLine, newLines);

            using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(modifiedContent));
            uploadStream.Position = 0;
            await client.UploadStream(uploadStream, remotePath, FtpRemoteExists.Overwrite);
        }
        catch (ObjectDisposedException)
        {
            _logger.LogError("FTP client was disposed during RemoveLine");
            throw;
        }
        catch (Exception ex)
        {
            throw new DomainException($"Failed to remove line from file: {ex.Message}", ex);
        }
    }

    public async Task AddLine(AsyncFtpClient client, string remotePath, string lineToAdd)
    {
        try
        {
            if (client.IsDisposed)
                throw new ObjectDisposedException("FTP client was disposed");

            using var downloadStream = new MemoryStream();
            if (await client.DownloadStream(downloadStream, remotePath))
            {
                downloadStream.Position = 0;
                using var reader = new StreamReader(downloadStream, Encoding.UTF8);
                string content = await reader.ReadToEndAsync();

                var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
                lines.Add(lineToAdd);

                string modifiedContent = string.Join("\n", lines);

                using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(modifiedContent));
                uploadStream.Position = 0;
                await client.UploadStream(uploadStream, remotePath, FtpRemoteExists.Overwrite);
            }
        }
        catch (ObjectDisposedException)
        {
            _logger.LogError("FTP client was disposed during AddLine");
            throw;
        }
        catch (Exception ex)
        {
            throw new DomainException($"Failed to add line to file: {ex.Message}", ex);
        }
    }

    private static string GetServerKey(Ftp ftp) => $"{ftp.UserName}@{ftp.Address}:{ftp.Port}";

    public async ValueTask DisposeAsync()
    {
        foreach (var semaphore in _serverSemaphores.Values)
        {
            semaphore.Dispose();
        }
        _serverSemaphores.Clear();
        _activeConnections.Clear();
    }
}
