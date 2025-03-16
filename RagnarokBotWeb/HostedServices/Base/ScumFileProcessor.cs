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
    private readonly IFtpService _ftpService;
    private readonly ILogger<ScumFileProcessor> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ScumFileProcessor(IServiceProvider serviceProvider,
        IFtpService ftpService)
    {
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ScumFileProcessor>();
        _serviceProvider = serviceProvider;
        _ftpService = ftpService;
    }

    public async Task SaveUnreadFileLines(ScumServer server, EFileType fileType)
    {
        _logger.LogInformation("{}->Execute SaveUnreadFileLines for server: {}", fileType, server.Id);
        var ftp = server.Ftp!;
        var client = _ftpService.GetClient(ftp);
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, server.GetTimeZoneOrDefault());
        var files = GetLogFiles(client, ftp.RootFolder, today, fileType);

        if (files.Count == 0)
        {
            _logger.LogInformation("No files found, ignoring ScumFileProcessor.SaveUnreadFileLines process.");
            return;
        }

        var pointer = GetFileReaderPointer(server, today, fileType);
        const int flushIn = 1000;

        pointer ??= new ReaderPointer
        {
            LineNumber = 0,
            FileType = fileType,
            FileName = files.First(),
            ScumServer = server,
            FileDate = ScumUtils.ParseDateTime(files.First())
        };

        var unreadFiles = GetUnreadFiles(pointer, files, [today]);

        foreach (var fileName in unreadFiles)
        {
            _logger.LogInformation("{}->Execute Reading file: {}", fileType, fileName);

            await using var stream = client.OpenRead(ftp.RootFolder + fileName);
            using var reader = new StreamReader(stream, Encoding.Unicode);

            var lines = new List<string>();
            var lineNumber = 0;
            var partialRead = false;

            if (pointer.FileName != fileName)
            {
                pointer.FileName = fileName;
                pointer.FileDate = ScumUtils.ParseDateTime(fileName);
                pointer.LineNumber = 0;
            }

            while (await reader.ReadLineAsync() is { } line)
            {
                lineNumber++;

                if (pointer.LineNumber > lineNumber && !partialRead)
                {
                    partialRead = true;
                    continue;
                }

                lines.Add(line);
                pointer.LineNumber = lineNumber;

                if (lines.Count != flushIn) continue;

                pointer = await SaveLines(pointer, fileName, lines, server);
                lines.Clear();
            }

            if (files.Count > 0) pointer = await SaveLines(pointer, fileName, lines, server);
        }
    }

    private static List<string> GetUnreadFiles(ReaderPointer? pointer, List<string> fileNames,
        List<DateTime> targetDateTimes)
    {
        if (pointer == null || pointer.Id == 0) return fileNames;
        if (!targetDateTimes.Select(x => x.Date).Contains(pointer.FileDate.Date)) return fileNames;

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

    private ReaderPointer? GetFileReaderPointer(ScumServer server, DateTime datetime, EFileType fileType)
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IReaderPointerService>();

        return service.GetReaderPointer(datetime, server.Id, fileType).Result;
    }

    private async Task<ReaderPointer> SaveLines(ReaderPointer pointer, string filename, IList<string> lines,
        ScumServer server)
    {
        using var scope = _serviceProvider.CreateScope();

        var readerPointerRepository = scope.ServiceProvider.GetRequiredService<IReaderPointerRepository>();
        await readerPointerRepository.CreateOrUpdateAsync(pointer);

        var readerRepository = scope.ServiceProvider.GetRequiredService<IReaderRepository>();
        foreach (var line in lines.Select(line => new Reader(filename, line, server)))
            await readerRepository.AddAsync(line);

        await readerPointerRepository.SaveAsync();
        await readerRepository.SaveAsync();

        return pointer;
    }
}