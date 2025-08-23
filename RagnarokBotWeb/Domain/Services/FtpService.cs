using FluentFTP;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.FTP;
using System.Net.Sockets;
using System.Text;

namespace RagnarokBotWeb.Domain.Services;

public class FtpService(FtpConnectionPool pool, ILogger<FtpServer> logger) : IFtpService
{
    private static readonly SemaphoreSlim _ftpLock = new(1, 1);

    public async Task<AsyncFtpClient> GetClientAsync(Ftp ftp, CancellationToken cancellationToken = default)
    {
        try
        {
            return await pool.GetClientAsync(ftp, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            // If we can't get a client, clear the pool for this FTP server and try once more
            await pool.ClearPoolAsync(ftp);
            throw new DomainException($"Unable to connect to FTP server: {ex.Message}", ex);
        }
    }

    public async Task CopyFilesAsync(AsyncFtpClient client, string targetFolder, IList<string> remoteFilePaths, CancellationToken token = default)
    {
        int retryCount = 0;
        const int maxRetries = 3;

        await _ftpLock.WaitAsync(token);
        try
        {
            List<FtpResult> results;

            do
            {
                results = await client.DownloadFiles(targetFolder, remoteFilePaths, FtpLocalExists.Overwrite, FtpVerify.Throw, token: token);

                if (results.Any(result => result.IsFailed))
                {
                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), token); // Exponential backoff
                    }
                }
            }
            while (results.Any(result => result.IsFailed) && retryCount < maxRetries);

            if (results.Any(result => result.IsFailed))
            {
                results.ForEach(result => logger.LogError(result.Exception, "CopyFilesAsync"));
                throw new DomainException("Error while copying files from FTP after multiple retries");
            }
        }
        finally
        {
            _ftpLock.Release();
        }
    }

    public async Task UpdateINILine(AsyncFtpClient client, string remoteFilePath, string key, string newValue)
    {
        try
        {
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
        catch (Exception ex)
        {
            throw new DomainException($"Failed to update INI file: {ex.Message}", ex);
        }
    }

    public async Task<Stream?> DownloadFile(AsyncFtpClient client, string remoteFilePath)
    {
        try
        {
            var stream = new MemoryStream();
            if (await client.DownloadStream(stream, remoteFilePath))
            {
                stream.Position = 0;
                return stream;
            }
            return null;
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
            using var downloadStream = new MemoryStream();
            if (await client.DownloadStream(downloadStream, remotePath))
            {
                downloadStream.Position = 0;
                using var reader = new StreamReader(downloadStream, Encoding.UTF8);
                string content = await reader.ReadToEndAsync();

                var newLines = content
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                    .Where(line => !line.Contains(lineToRemove));

                string modifiedContent = string.Join("\n", newLines);

                using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(modifiedContent));
                uploadStream.Position = 0;
                await client.UploadStream(uploadStream, remotePath, FtpRemoteExists.Overwrite);
            }
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
        catch (Exception ex)
        {
            throw new DomainException($"Failed to add line to file: {ex.Message}", ex);
        }
    }

    public async Task ReleaseClientAsync(AsyncFtpClient client)
    {
        await pool.ReleaseClientAsync(client);
    }

    /// <summary>
    /// Clears the connection pool for a specific FTP server (useful when server connectivity issues are detected)
    /// </summary>
    public async Task ClearPoolForServerAsync(Ftp ftp)
    {
        await pool.ClearPoolAsync(ftp);
    }

    /// <summary>
    /// Handles FTP operation with automatic retry and pool clearing on failure
    /// </summary>
    public async Task<T> ExecuteWithRetryAsync<T>(Ftp ftp, Func<AsyncFtpClient, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        AsyncFtpClient? client = null;
        try
        {
            client = await GetClientAsync(ftp, cancellationToken);
            return await operation(client);
        }
        catch (ObjectDisposedException)
        {
            // Clear pool and try once more
            await pool.ClearPoolAsync(ftp);

            if (client != null)
            {
                // Don't return this client to the pool since it's likely bad
                await pool.ReleaseClientAsync(client);
                client = null;
            }

            try
            {
                client = await GetClientAsync(ftp, cancellationToken);
                return await operation(client);
            }
            catch (Exception retryEx)
            {
                throw new DomainException($"FTP operation failed after retry: {retryEx.Message}", retryEx);
            }
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            // Clear pool and try once more
            await pool.ClearPoolAsync(ftp);

            if (client != null)
            {
                // Don't return this client to the pool since it's likely bad
                await pool.ReleaseClientAsync(client);
                client = null;
            }

            try
            {
                client = await GetClientAsync(ftp, cancellationToken);
                return await operation(client);
            }
            catch (Exception retryEx)
            {
                throw new DomainException($"FTP operation failed after retry: {retryEx.Message}", retryEx);
            }
        }
        finally
        {
            if (client != null)
            {
                await pool.ReleaseClientAsync(client);
            }
        }
    }

    private static bool IsNetworkError(Exception ex)
    {
        return ex is TimeoutException ||
               ex is SocketException ||
               ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase);
    }
}