using System.Text;
using FluentFTP;
using RagnarokBotWeb.Crosscutting.Utils;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.HostedServices.Base;

public class ScumFileProcessor
{

    private static readonly Func<string, string> LocalPathFunc =
        server => Path.Combine(Path.GetTempPath(), "ragnarok", server, "ftp_files");

    private readonly ILogger<ScumFileProcessor> _logger;
    
    private readonly IServiceProvider _serviceProvider;
    private readonly IFtpService _ftpService;
    
    private readonly ScumServer _scumServer;
    private readonly EFileType _fileType;
    private readonly Ftp _ftp;
    
    public ScumFileProcessor(
        IServiceProvider serviceProvider,
        IFtpService ftpService,
        ScumServer server,
        EFileType fileType
    )
    {
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ScumFileProcessor>();
        _serviceProvider = serviceProvider;
        _ftpService = ftpService;
        _scumServer = server;
        _fileType = fileType;
        _ftp = server.Ftp!;
    }

    public async Task<IList<string>> ProcessUnreadFileLines()
    {
        _logger.LogInformation("{}->Execute ProcessUnreadFileLines for server: {}", _fileType, _scumServer.Id);

        var client = _ftpService.GetClient(_ftp);
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _scumServer.GetTimeZoneOrDefault());
        var ftpFileNames = GetLogFiles(client, _ftp.RootFolder, today, _fileType);

        if (ftpFileNames.Count == 0)
        {
            _logger.LogWarning("No files found, ignoring ProcessUnreadFileLines process.");
            return [];
        }

        var localPath = GetLocalPath();
        Directory.CreateDirectory(localPath);

        var pointer = await GetReaderPointer();
        var ftpUnreadFileNames = GetUnreadFiles(pointer, ftpFileNames, today);

        if (ftpUnreadFileNames.Count == 0)
        {
            _logger.LogWarning("No files unread found, ignoring ProcessUnreadFileLines process.");
            return [];
        }

        _ftpService.CopyFiles(client, localPath, GetFilePaths(_ftp.RootFolder, ftpUnreadFileNames));

        List<string> allLines = [];
        
        var index = 0;
        do
        {
            var filename = ftpUnreadFileNames[index];
            var filepath = Path.Combine(localPath, filename);
            
            pointer ??= BuildReaderPointer(filepath);
            
            if (pointer.FileName != filename)
            {
                pointer.FileName = filename;
                pointer.LineNumber = 0;
                pointer.FileDate = ScumUtils.ParseDateTime(filename);
            }
            
            var lines = await ReadFileLines(pointer, filepath);
            allLines.AddRange(lines);
            
            index++;
            
            pointer = await GetReaderPointer();
        } while (ftpUnreadFileNames.Count > index);

        return allLines;
    }

    private string GetLocalPath()
    {
        return LocalPathFunc.Invoke($"server_{_scumServer.Id}");
    }

    private async Task<IList<string>> ReadFileLines(ReaderPointer pointer, string filepath)
    {
        var filename = Path.GetFileName(filepath);
        
        _logger.LogInformation("{}->Execute Reading file: {}", _fileType, filename);

        using var reader = new StreamReader(filepath, Encoding.Unicode);

        var lines = new List<string>();
        var lineNumber = 0;

        while (await reader.ReadLineAsync() is { } line)
        {
            lineNumber++;

            if (pointer.LineNumber >= lineNumber) continue;

            lines.Add(line);
        }

        if (lines.Count > 0)
        {
            pointer.LineNumber = lineNumber;
            await SaveLines(pointer, filename, lines);
        };

        return lines;
    }

    private ReaderPointer BuildReaderPointer(string filepath)
    {
        var filename = Path.GetFileName(filepath);
        return new ReaderPointer
        {
            LineNumber = 0,
            FileType = _fileType,
            FileName = filename,
            ScumServer = _scumServer,
            FileDate = ScumUtils.ParseDateTime(filename)
        };
    }

    private static List<string> GetUnreadFiles(ReaderPointer? pointer, List<string> fileNames, DateTime targetDateTime)
    {
        if (pointer == null || pointer.Id == 0) return fileNames;
        if (targetDateTime.Date != pointer.FileDate.Date) return fileNames;

        var index = fileNames.TakeWhile(fileName => pointer.FileDate > ScumUtils.ParseDateTime(fileName)).Count();

        return fileNames.GetRange(index, fileNames.Count - index);
    }

    private static List<string> GetLogFiles(FtpClient client, string? rootFolder, DateTime today, EFileType fileType)
    {
        var prefixFileName = fileType.ToString().ToLower() + "_" + today.ToString("yyyyMMdd");

        return client.GetNameListing(rootFolder)
            .ToList()
            .Where(fileName => fileName.StartsWith(prefixFileName))
            .OrderBy(ScumUtils.ParseDateTime)
            .ToList();
    }

    private Task<ReaderPointer?> GetReaderPointer()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IReaderPointerService>();

        return service.GetReaderPointer(_scumServer.Id, _fileType);
    }

    private async Task SaveLines(ReaderPointer pointer, string filename, IList<string> lines)
    {
        using var scope = _serviceProvider.CreateScope();

        var readerPointerRepository = scope.ServiceProvider.GetRequiredService<IReaderPointerRepository>();
        await readerPointerRepository.CreateOrUpdateAsync(pointer);

        var readerRepository = scope.ServiceProvider.GetRequiredService<IReaderRepository>();
        await readerRepository.AddRangeAsync(lines.Select(line => new Reader(filename, line, _scumServer)).ToList());

        await readerPointerRepository.SaveAsync();
        await readerRepository.SaveAsync();
    }

    private static IList<string> GetFilePaths(string? path, IList<string> filenames)
    {
        return path == null ? filenames : filenames.Select(filename => Path.Combine(path, filename)).ToList();
    }
}