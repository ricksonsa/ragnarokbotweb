using FluentFTP;
using RagnarokBotWeb.Crosscutting.Utils;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Serilog;
using System.Diagnostics;
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

    private static List<FtpListItem> GetLogFiles(FtpClient client, string? rootFolder, DateTime today, EFileType fileType)
    {
        var prefixFileNameYesterday = fileType.ToString().ToLower() + "_" + today.AddDays(-1).ToString("yyyyMMdd");
        var prefixFileNameToday = fileType.ToString().ToLower() + "_" + today.ToString("yyyyMMdd");
        var prefixFileNameTomorrow = fileType.ToString().ToLower() + "_" + today.AddDays(+1).ToString("yyyyMMdd");

        try
        {
            return client.GetListing(rootFolder + "/Saved/SaveFiles/Logs/")
              .Where(file => file.Name.StartsWith(prefixFileNameYesterday)
                            || file.Name.StartsWith(prefixFileNameToday)
                            || file.Name.StartsWith(prefixFileNameTomorrow))
              .OrderBy(file => file.Created)
              .ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            throw;
        }
    }

    private string GetLocalPath()
    {
        return LocalPathFunc.Invoke($"server_{_scumServer.Id}");
    }

    private ReaderPointer BuildReaderPointer(string filepath, DateTime modified)
    {
        var fileInfo = new FileInfo(filepath);
        return new ReaderPointer
        {
            LineNumber = 0,
            FileName = fileInfo.Name,
            FileSize = fileInfo.Length,
            LastUpdated = modified,
            ScumServer = _scumServer,
            FileDate = ScumUtils.ParseDateTime(fileInfo.Name)
        };
    }

    public async IAsyncEnumerable<string> UnreadFileLinesAsync()
    {
        FtpClient client = _ftpService.GetClient(_ftp);
        DateTime today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _scumServer.GetTimeZoneOrDefault());

        List<FtpListItem> ftpFiles = GetLogFiles(client, _ftp.RootFolder, today, _fileType);

        List<(FtpListItem, ReaderPointer?)> filteredFiles = [];
        foreach (var file in ftpFiles)
        {
            var pointer = await _readerPointerRepository.FindOneAsync(p => p.FileName == file.Name);
            if (pointer == null || pointer.LastUpdated != file.Modified)
            {
                filteredFiles.Add((file, pointer));
            }
        }

        if (filteredFiles.Count == 0)
        {
            _logger.LogWarning("No unread or updated files found. Skipping UnreadFileLinesAsync.");
            yield break;
        }

        string localPath = GetLocalPath();
        Directory.CreateDirectory(localPath);

        _ftpService.CopyFiles(client, localPath, filteredFiles.Select(f => f.Item1.FullName).ToList());

        foreach (var file in filteredFiles)
        {
            string filePath = Path.Combine(localPath, file.Item1!.Name);
            ReaderPointer pointer = file.Item2 ?? BuildReaderPointer(filePath, file.Item1?.Modified ?? DateTime.Now)!;

            int lineNumber = 0;
            using var reader = new StreamReader(filePath, Encoding.Unicode);

            while (await reader.ReadLineAsync() is { } line)
            {
                lineNumber++;

                if (pointer!.LineNumber >= lineNumber) continue;
                if (string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line) || line.Contains("Game version")) continue;

                _logger.LogDebug("Reading file File[{FileName}:{FileModified}] returning line {Line}", file.Item1!.Name, file.Item1!.Modified, line);
                yield return line;
            }

            pointer!.LineNumber = lineNumber;

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
}