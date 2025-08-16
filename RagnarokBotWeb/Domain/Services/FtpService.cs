using FluentFTP;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.FTP;
using System.Text;

namespace RagnarokBotWeb.Domain.Services;

public class FtpService(FtpConnectionPool pool) : IFtpService
{
    private static readonly SemaphoreSlim _ftpLock = new(1, 1);

    public async Task<AsyncFtpClient> GetClientAsync(Ftp ftp, CancellationToken cancellationToken = default)
    {
        return await pool.GetClientAsync(ftp, cancellationToken: cancellationToken);
    }

    public async Task CopyFilesAsync(AsyncFtpClient client, string targetFolder, IList<string> remoteFilePaths, CancellationToken token = default)
    {
        int retryCount = 0;
        await _ftpLock.WaitAsync(token);
        var states = await client.DownloadFiles(targetFolder, remoteFilePaths, FtpLocalExists.Overwrite, FtpVerify.Throw, token: token);
        if (states.Any(result => result.IsFailed))
        {
            while (retryCount < 3)
            {
                retryCount += 1;
                states = await client.DownloadFiles(targetFolder, remoteFilePaths, FtpLocalExists.Overwrite, FtpVerify.Throw, token: token);
            }
        }
        if (states.Any(result => result.IsFailed)) throw new Exception("Error while copying files from FTP");

        _ftpLock.Release();
    }

    public async Task UpdateINILine(AsyncFtpClient client, string remoteFilePath, string key, string newValue)
    {
        try
        {
            //string tempLocalPath = "temp_file.txt"; // Temporary local file
            //string remoteFilePath = $"{ftp.RootFolder}/Saved/Config/WindowsServer/ServerSettings.ini

            using (MemoryStream stream = new())
            {
                await client.DownloadStream(stream, remoteFilePath);
                stream.Position = 0;

                var content = await new StreamReader(stream).ReadToEndAsync();
                string[] lines = content.Split(Environment.NewLine);

                int lineIndex = Array.FindIndex(lines, line => line.Contains(key));

                if (lineIndex != -1)
                {
                    lines[lineIndex] = $"{lines[lineIndex].Split("=")[0]}={newValue}"; // Replace line

                    string updatedContent = string.Join(Environment.NewLine, lines);
                    MemoryStream updatedStream = new(Encoding.UTF8.GetBytes(updatedContent));

                    await client.UploadStream(updatedStream, remoteFilePath);
                }
            }
        }
        catch (Exception)
        {
            throw new DomainException("Invalid ftp server");
        }
    }


    public async Task<Stream?> DownloadFile(AsyncFtpClient client, string remoteFilePath)
    {
        try
        {
            MemoryStream stream = new();
            if (await client.DownloadStream(stream, remoteFilePath))
            {
                stream.Position = 0;
                await client.Disconnect();
                return stream;
            }
            return null;

        }
        catch (Exception)
        {
            throw new DomainException("Invalid ftp server");
        }
    }
    public async Task RemoveLine(AsyncFtpClient client, string remotePath, string lineToRemove)
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

        await client.Disconnect();
    }

    public async Task AddLine(AsyncFtpClient client, string remotePath, string lineToAdd)
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

        await client.Disconnect();
    }

    public async Task ReleaseClientAsync(AsyncFtpClient client)
    {
        await pool.ReleaseClientAsync(client);
    }
}