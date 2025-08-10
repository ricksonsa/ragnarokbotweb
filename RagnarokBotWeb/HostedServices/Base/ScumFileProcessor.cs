using FluentFTP;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Serilog;
using System.Text;

namespace RagnarokBotWeb.HostedServices.Base;

public class ScumFileProcessor
{

    private static readonly Func<string, string> LocalPathFunc =
        server => Path.Combine(Path.GetTempPath(), "ragnarok", server, "ftp_files");

    private readonly ILogger<ScumFileProcessor> _logger;
    private readonly ScumServer _scumServer;
    private readonly Ftp _ftp;

    public ScumFileProcessor(ScumServer server)
    {
        var loggerFactory = new LoggerFactory();
        loggerFactory.AddSerilog();
        _logger = loggerFactory.CreateLogger<ScumFileProcessor>();
        _scumServer = server;
        _ftp = server.Ftp!;
    }

    private static bool IsIrrelevantLine(string line)
    {
        return string.IsNullOrWhiteSpace(line) || line.Contains("Game version");
    }

    private void DeleteLocalFiles(string localPath, IEnumerable<string> files)
    {
        foreach (var file in files)
        {
            var path = Path.Combine(localPath + file);
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to delete file {Path} with error {Ex}", path, ex.Message);
            }
        }
    }

    private List<FtpListItem> GetLogFiles(FtpClient client, string? rootFolder, EFileType fileType)
    {
        DateTime today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _scumServer.GetTimeZoneOrDefault());
        var from = today.AddDays(-10);

        try
        {
            return client.GetListing(rootFolder + "/Saved/SaveFiles/Logs/",
                          FtpListOption.Modify | FtpListOption.Size)
              .Where(file => file.Name.StartsWith(fileType.ToString().ToLower() + "_") && file.RawModified.Date >= from && file.RawModified.Date <= today.AddDays(2))
              .OrderBy(file => file.RawModified)
              .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error GetLogFiles type {Type} -> {Ex}", fileType.ToString(), ex.Message);
            throw;
        }
    }

    private List<FtpListItem> GetLogFiles(FtpClient client, string? rootFolder, DateTime from, DateTime to, EFileType fileType)
    {
        try
        {
            return client.GetListing(rootFolder + "/Saved/SaveFiles/Logs/",
                          FtpListOption.Modify | FtpListOption.Size)
              .Where(file => file.Name.StartsWith(fileType.ToString().ToLower() + "_") && file.RawModified.Date >= from && file.RawModified.Date <= to)
              .OrderBy(file => file.RawModified)
              .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error GetLogFiles type {Type} -> {Ex}", fileType.ToString(), ex.Message);
            throw;
        }
    }

    private string GetLocalPath()
    {
        return LocalPathFunc.Invoke($"server_{_scumServer.Id}");
    }

    private ReaderPointer BuildReaderPointer(FtpListItem item)
    {
        return new ReaderPointer
        {
            LineNumber = 0,
            FileName = item.Name,
            FileSize = item.Size,
            LastUpdated = item.Modified,
            ScumServer = _scumServer,
            FileDate = DateTime.Now
        };
    }

    private ReaderPointer UpdateReaderPointer(ReaderPointer pointer, FtpListItem item)
    {
        pointer.FileSize = item.Size;
        pointer.LastUpdated = item.Modified;
        return pointer;
    }

    public async IAsyncEnumerable<string> UnreadFileLinesAsync(
        EFileType fileType,
        IReaderPointerRepository readerPointerRepository,
        IFtpService ftpService)
    {
        if (_ftp is null) yield break;

        FtpClient client = ftpService.GetClient(_ftp);
        List<FtpListItem> ftpFiles = GetLogFiles(client, _ftp.RootFolder, fileType);
        List<(FtpListItem, ReaderPointer?)> filteredFiles = [];

        // Filter to read only files that are new or were modified
        foreach (var file in ftpFiles)
        {
            _logger.LogDebug("File fetched {File}", file.Name);
            var pointer = await readerPointerRepository.FindOneAsync(p => p.FileName == file.Name);
            if (pointer == null || pointer.LastUpdated != file.Modified || pointer.FileSize != file.Size)
            {
                filteredFiles.Add((file, pointer));
            }
        }

        if (filteredFiles.Count == 0)
        {
            _logger.LogDebug("No unread or updated files found for {Type}. Skipping UnreadFileLinesAsync.", fileType);
            yield break;
        }

        string localPath = GetLocalPath();
        Directory.CreateDirectory(localPath);
        try
        {
            ftpService.CopyFiles(client, localPath, filteredFiles.Select(f => f.Item1.FullName).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError("Error trying to copy files");
            _logger.LogError("Exception [{Ex}]", ex.Message);
            _logger.LogError("Inner Exception [{Ex}]", ex.InnerException?.Message);
            throw;
        }

        foreach (var file in filteredFiles)
        {
            ReaderPointer pointer = file.Item2 is not null ? UpdateReaderPointer(file.Item2, file.Item1) : BuildReaderPointer(file.Item1);
            string filePath = Path.Combine(localPath, file.Item1!.Name);

            int lineNumber = 0;
            using var reader = new StreamReader(filePath, Encoding.Unicode);

            while (await reader.ReadLineAsync() is { } line)
            {
                if (lineNumber < pointer.LineNumber)
                {
                    lineNumber++;
                    continue;
                }

                if (IsIrrelevantLine(line))
                {
                    lineNumber++;
                    continue;
                }

                yield return line;
                _logger.LogDebug("Reading file File[{FileName}:{FileModified}]", file.Item1!.Name, file.Item1!.Modified);
                _logger.LogDebug("Yielding: {V}", line);

                lineNumber++;
            }

            if (lineNumber != pointer.LineNumber)
            {
                pointer.LineNumber = lineNumber;

                try
                {
                    await readerPointerRepository.CreateOrUpdateAsync(pointer);
                    await readerPointerRepository.SaveAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError("UnreadFileLinesAsync error {Exception}", ex);
                }
            }
        }

        client.Disconnect();
    }

    public async IAsyncEnumerable<string> FileLinesAsync(
       EFileType fileType,
       IFtpService ftpService,
       DateTime from,
       DateTime to)
    {
        if (_ftp is null) yield break;

        FtpClient client = ftpService.GetClient(_ftp);
        List<FtpListItem> ftpFiles = GetLogFiles(client, _ftp.RootFolder, from, to, fileType);
        List<FtpListItem> filteredFiles = [];

        string localPath = GetLocalPath();
        Directory.CreateDirectory(localPath);

        foreach (FtpListItem ftpFile in ftpFiles)
        {
            var localFile = new FileInfo(Path.Combine(localPath, ftpFile.Name));
            if (!File.Exists(localFile.FullName) || ftpFile.Size != localFile.Length)
                filteredFiles.Add(ftpFile);
        }

        try
        {
            ftpService.CopyFiles(client, localPath, filteredFiles.Select(f => f.FullName).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError("Error trying to copy files");
            _logger.LogError("Exception [{Ex}]", ex.Message);
            _logger.LogError("Inner Exception [{Ex}]", ex.InnerException?.Message);
            throw;
        }

        foreach (var file in ftpFiles)
        {
            var localFile = new FileInfo(Path.Combine(localPath, file.Name));
            int lineNumber = 0;
            using var reader = new StreamReader(localFile.FullName, Encoding.Unicode);
            while (await reader.ReadLineAsync() is { } line)
            {
                if (IsIrrelevantLine(line))
                {
                    lineNumber++;
                    continue;
                }

                yield return line;
                lineNumber++;
            }
        }

        client.Disconnect();
    }

    public Task<string> DownloadRaidTimes(IFtpService ftpService)
    {
        var client = ftpService.GetClient(_ftp);
        var remotePath = $"{_ftp.RootFolder}/Saved/Config/WindowsServer/RaidTimes.json";
        var localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ragnarokbot", "servers", _scumServer.Id.ToString());
        ftpService.CopyFiles(client, localPath, [remotePath]);
        _logger.LogDebug("Downloaded RaidTimes.json from server {Server}", _scumServer.Id);
        using var reader = new StreamReader(Path.Combine(localPath, "RaidTimes.json"));
        return reader.ReadToEndAsync();
    }

    public Task<string> ReadLocalRaidTimesAsync(CancellationToken token = default)
    {
        var localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ragnarokbot", "servers", _scumServer.Id.ToString(), "RaidTimes.json");
        using var reader = new StreamReader(localPath);
        return reader.ReadToEndAsync(token);
    }

    public Task SaveSquadList(string squadList)
    {
        var localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ragnarokbot", "servers", _scumServer.Id.ToString(), "squadlist.json");
        _logger.LogDebug("Saving SquadList from server {Server}", _scumServer.Id);
        using var reader = new StreamWriter(localPath, false);
        return reader.WriteAsync(squadList);
    }

    public Task<string> ReadSquadListAsync(CancellationToken token = default)
    {
        var localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ragnarokbot", "servers", _scumServer.Id.ToString(), "squadlist.json");
        using var reader = new StreamReader(localPath);
        return reader.ReadToEndAsync(token);
    }

    public Task SaveFlagList(string flagList)
    {
        var localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ragnarokbot", "servers", _scumServer.Id.ToString(), "flaglist.json");
        _logger.LogDebug("Saving FlagList from server {Server}", _scumServer.Id);
        using var reader = new StreamWriter(localPath, false);
        return reader.WriteAsync(flagList);
    }

    public Task<string> ReadFlagListAsync(CancellationToken token = default)
    {
        var localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ragnarokbot", "servers", _scumServer.Id.ToString(), "flaglist.json");
        using var reader = new StreamReader(localPath);
        return reader.ReadToEndAsync(token);
    }
}