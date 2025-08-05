using FluentFTP;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.FTP;
using System.Text;

namespace RagnarokBotWeb.Domain.Services;

public class FtpService(FtpConnectionPool pool) : IFtpService
{
    public FtpClient GetClient(Ftp ftp)
    {
        return pool.GetClient(ftp);
    }

    public void CopyFiles(FtpClient client, string targetFolder, IList<string> remoteFilePaths)
    {
        var states = client.DownloadFiles(targetFolder, remoteFilePaths, FtpLocalExists.Overwrite, FtpVerify.Throw);
        if (states.Any(result => result.IsFailed)) throw new Exception("Error while copying files from FTP");
    }

    public async Task UpdateINILine(FtpClient client, string remoteFilePath, string key, string newValue)
    {
        try
        {
            //string tempLocalPath = "temp_file.txt"; // Temporary local file
            //string remoteFilePath = $"{ftp.RootFolder}/Saved/Config/WindowsServer/ServerSettings.ini

            using (MemoryStream stream = new())
            {
                client.DownloadStream(stream, remoteFilePath);
                stream.Position = 0;

                var content = await new StreamReader(stream).ReadToEndAsync();
                string[] lines = content.Split(Environment.NewLine);

                int lineIndex = Array.FindIndex(lines, line => line.Contains(key));

                if (lineIndex != -1)
                {
                    lines[lineIndex] = $"{lines[lineIndex].Split("=")[0]}={newValue}"; // Replace line

                    string updatedContent = string.Join(Environment.NewLine, lines);
                    MemoryStream updatedStream = new(Encoding.UTF8.GetBytes(updatedContent));

                    client.UploadStream(updatedStream, remoteFilePath);
                }
            }
            client.Disconnect();

        }
        catch (Exception)
        {
            throw new DomainException("Invalid ftp server");
        }
    }


    public Stream? DownloadFile(FtpClient client, string remoteFilePath)
    {
        try
        {
            MemoryStream stream = new();
            if (client.DownloadStream(stream, remoteFilePath))
            {
                stream.Position = 0;
                client.Disconnect();
                return stream;
            }
            return null;

        }
        catch (Exception)
        {
            throw new DomainException("Invalid ftp server");
        }
    }
    public async Task RemoveLine(FtpClient client, string remotePath, string lineToRemove)
    {
        using var downloadStream = new MemoryStream();
        if (client.DownloadStream(downloadStream, remotePath))
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
            client.UploadStream(uploadStream, remotePath, FtpRemoteExists.Overwrite);
        }

        client.Disconnect();
    }

    public async Task AddLine(FtpClient client, string remotePath, string lineToAdd)
    {
        using var downloadStream = new MemoryStream();
        if (client.DownloadStream(downloadStream, remotePath))
        {
            downloadStream.Position = 0;
            using var reader = new StreamReader(downloadStream, Encoding.UTF8);
            string content = await reader.ReadToEndAsync();

            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
            lines.Add(lineToAdd);

            string modifiedContent = string.Join("\n", lines);

            using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(modifiedContent));
            uploadStream.Position = 0;
            client.UploadStream(uploadStream, remotePath, FtpRemoteExists.Overwrite);
        }

        client.Disconnect();
    }

    public Task<Stream> DownloadFile(FtpClient client)
    {
        throw new NotImplementedException();
    }
}