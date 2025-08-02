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
    private readonly IReaderPointerRepository _readerPointerRepository;
    private readonly IFtpService _ftpService;

    private readonly ScumServer _scumServer;
    private readonly EFileType _fileType;
    private readonly Ftp _ftp;

    public ScumFileProcessor(
        IFtpService ftpService,
        ScumServer server,
        EFileType fileType,
        IReaderPointerRepository readerPointerRepository)
    {
        var loggerFactory = new LoggerFactory();
        loggerFactory.AddSerilog();
        _logger = loggerFactory.CreateLogger<ScumFileProcessor>();
        _readerPointerRepository = readerPointerRepository;
        _ftpService = ftpService;
        _scumServer = server;
        _fileType = fileType;
        _ftp = server.Ftp!;
    }

    private List<FtpListItem> GetLogFiles(FtpClient client, string? rootFolder, DateTime today, EFileType fileType)
    {
        var prefixFileNameYesterday = fileType.ToString().ToLower() + "_" + today.AddDays(-1).ToString("yyyyMMdd");
        var prefixFileNameToday = fileType.ToString().ToLower() + "_" + today.ToString("yyyyMMdd");
        var prefixFileNameTomorrow = fileType.ToString().ToLower() + "_" + today.AddDays(+1).ToString("yyyyMMdd");

        try
        {
            return client.GetListing(rootFolder + "/Saved/SaveFiles/Logs/",
                          FtpListOption.Modify | FtpListOption.Size)
               .Where(file => file.Name.StartsWith(prefixFileNameYesterday)
                            || file.Name.StartsWith(prefixFileNameToday)
                            || file.Name.StartsWith(prefixFileNameTomorrow))
              .OrderBy(file => file.Created)
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

    public async IAsyncEnumerable<string> UnreadFileLinesAsync()
    {
        if (_ftp is null) yield break;

        FtpClient client = _ftpService.GetClient(_ftp);
        DateTime today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _scumServer.GetTimeZoneOrDefault());

        List<FtpListItem> ftpFiles = GetLogFiles(client, _ftp.RootFolder, today, _fileType);

        List<(FtpListItem, ReaderPointer?)> filteredFiles = [];

        // Filter to read only files that are new or were modified
        foreach (var file in ftpFiles)
        {
            _logger.LogDebug("File fetched {File}", file.Name);
            var pointer = await _readerPointerRepository.FindOneAsync(p => p.FileName == file.Name);
            if (pointer == null || pointer.LastUpdated != file.Modified || pointer.FileSize != file.Size)
            {
                filteredFiles.Add((file, pointer));
            }
        }

        if (filteredFiles.Count == 0)
        {
            _logger.LogDebug("No unread or updated files found for {Type}. Skipping UnreadFileLinesAsync.", _fileType);
            yield break;
        }

        string localPath = GetLocalPath();
        Directory.CreateDirectory(localPath);

        DeleteLocalFiles(localPath, filteredFiles.Select(f => f.Item1.Name));
        try
        {
            _ftpService.CopyFiles(client, localPath, filteredFiles.Select(f => f.Item1.FullName).ToList());
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

                if (string.IsNullOrWhiteSpace(line) || line.Contains("Game version"))
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
                    await _readerPointerRepository.CreateOrUpdateAsync(pointer);
                    await _readerPointerRepository.SaveAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError("UnreadFileLinesAsync error {Exception}", ex);
                }
            }
        }

        client.Disconnect();
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
}