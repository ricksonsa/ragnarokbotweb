using FluentFTP;
using RagnarokBotWeb.Crosscutting.Utils;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using System.Diagnostics;
using System.Text;

namespace RagnarokBotWeb.HostedServices.Base;

public class ScumFileProcessor
{

    private static readonly Func<string, string> LocalPathFunc =
        server => Path.Combine(Path.GetTempPath(), "ragnarok", server, "ftp_files");

    private readonly ILogger<ScumFileProcessor> _logger;
    private readonly IReaderPointerRepository _readerPointerRepository;
    private readonly IReaderRepository _readerRepository;
    private readonly IScumServerRepository _scumServerRepository;
    private readonly IFtpService _ftpService;

    private readonly ScumServer _scumServer;
    private readonly EFileType _fileType;
    private readonly Ftp _ftp;

    public ScumFileProcessor(
        IFtpService ftpService,
        ScumServer server,
        EFileType fileType,
        IReaderPointerRepository readerPointerRepository,
        IScumServerRepository scumServerRepository,
        IReaderRepository readerRepository)
    {
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ScumFileProcessor>();
        _readerPointerRepository = readerPointerRepository;
        _ftpService = ftpService;
        _scumServer = server;
        _fileType = fileType;
        _ftp = server.Ftp!;
        _scumServerRepository = scumServerRepository;
        _readerRepository = readerRepository;
    }

    public async IAsyncEnumerable<string> UnreadFileLinesAsync()
    {
        _logger.LogInformation("{} -> Executing ProcessUnreadFileLines for server: {}", _fileType, _scumServer.Id);

        var client = _ftpService.GetClient(_ftp);
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _scumServer.GetTimeZoneOrDefault());

        var ftpFiles = GetLogFiles(client, _ftp.RootFolder, today, _fileType);

        // Filter files with no changes in size
        List<(FtpListItem, ReaderPointer?)> filteredFiles = [];
        foreach (var file in ftpFiles)
        {
            var pointer = await _readerPointerRepository.FindOneAsync(p => p.FileName == file.Name);
            if (pointer == null || pointer.FileSize != file.Size)
            {
                filteredFiles.Add((file, pointer));
            }
        }

        if (filteredFiles.Count == 0)
        {
            _logger.LogWarning("No unread or updated files found. Skipping ProcessUnreadFileLines.");
            yield break;
        }

        var localPath = GetLocalPath();
        Directory.CreateDirectory(localPath);

        // Download required files
        _ftpService.CopyFiles(client, localPath, filteredFiles.Select(f => f.Item1.FullName).ToList());

        var allLines = new List<string>();
        foreach (var file in filteredFiles)
        {
            var filePath = Path.Combine(localPath, file.Item1.Name);
            var pointer = file.Item2 ?? BuildReaderPointer(filePath);

            var filename = Path.GetFileName(filePath);
            _logger.LogInformation("{} -> Reading file: {}", _fileType, filename);

            int lineNumber = 0;
            using var reader = new StreamReader(filePath, Encoding.Unicode);

            while (await reader.ReadLineAsync() is { } line)
            {
                lineNumber++;

                if (pointer.LineNumber >= lineNumber) continue;

                yield return line;
            }

            pointer.LineNumber = lineNumber;
            await _readerPointerRepository.CreateOrUpdateAsync(pointer);
            await _readerPointerRepository.SaveAsync();
        }
    }

    private string GetLocalPath()
    {
        return LocalPathFunc.Invoke($"server_{_scumServer.Id}");
    }

    private ReaderPointer BuildReaderPointer(string filepath)
    {
        var fileInfo = new FileInfo(filepath);
        return new ReaderPointer
        {
            LineNumber = 0,
            FileName = fileInfo.Name,
            FileSize = fileInfo.Length,
            ScumServer = _scumServer,
            FileDate = ScumUtils.ParseDateTime(fileInfo.Name)
        };
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
#if DEBUG
            Debug.WriteLine(ex.Message);
#endif
            throw;
        }

    }
}