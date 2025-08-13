using FluentFTP;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Serilog;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

namespace RagnarokBotWeb.HostedServices.Base;

public class ScumFileProcessor
{
    private static readonly Func<string, string> AppDataPathFunc =
        server => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", server);

    private readonly ILogger<ScumFileProcessor> _logger;
    private readonly ScumServer _scumServer;
    private readonly Ftp _ftp;
    private static readonly ConcurrentDictionary<(EFileType, long), SemaphoreSlim> _semaphores = [];
    private static readonly SemaphoreSlim _ftpLock = new(1, 1);

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

    private async Task<List<FtpListItem>> GetLogFiles(AsyncFtpClient client, string? rootFolder, EFileType fileType)
    {
        var today = DateTime.UtcNow;

        try
        {
            return (await client.GetListing(rootFolder + "/Saved/SaveFiles/Logs/", FtpListOption.Modify | FtpListOption.Size))
              .Where(file => file.Name.StartsWith(fileType.ToString().ToLower())
              && file.RawModified.Month == today.Month
              || file.RawModified.Month == today.Month - 1
              || file.RawModified.Month == today.Month + 1)
              .OrderBy(file => file.RawModified)
              .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error GetLogFiles type {Type} -> {Ex}", fileType.ToString(), ex.Message);
            throw;
        }
    }

    private async Task<List<FtpListItem>> GetLogFiles(AsyncFtpClient client, string? rootFolder, DateTime from, DateTime to, EFileType fileType)
    {
        try
        {
            return (await client.GetListing(rootFolder + "/Saved/SaveFiles/Logs/", FtpListOption.Modify | FtpListOption.Size))
              .Where(file => file.Name.StartsWith(fileType.ToString().ToLower() + "_") && file.RawModified.Date >= from && file.RawModified.Date <= to)
              .OrderBy(file => file.RawModified)
              .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error GetLogFiles type {Type} -> {Ex}", fileType.ToString(), ex.Message);
            throw;
        }
    }

    private string GetLocalPath()
    {
        return AppDataPathFunc.Invoke($"server_{_scumServer.Id}");
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

    private async IAsyncEnumerable<string> ReadLinesFromFileAsync(
    string filePath,
    ReaderPointer currentPointer,
    IReaderPointerRepository readerPointerRepository,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int lineNumber = 0;
        int linesSinceLastPointerUpdate = 0;
        const int PointerUpdateInterval = 100;

        using var reader = new StreamReader(filePath, Encoding.UTF8);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync();

            if (line == null)
                break;

            if (lineNumber < currentPointer.LineNumber)
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

            lineNumber++;
            linesSinceLastPointerUpdate++;

            if (linesSinceLastPointerUpdate >= PointerUpdateInterval)
            {
                currentPointer.LineNumber = lineNumber;
                try
                {
                    await readerPointerRepository.CreateOrUpdateAsync(currentPointer);
                    await readerPointerRepository.SaveAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating reader pointer during file read {FileName}", Path.GetFileName(filePath));
                }
                linesSinceLastPointerUpdate = 0;
            }
        }

        if (lineNumber != currentPointer.LineNumber)
        {
            currentPointer.LineNumber = lineNumber;
            try
            {
                await readerPointerRepository.CreateOrUpdateAsync(currentPointer);
                await readerPointerRepository.SaveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reader pointer after file read {FileName}", Path.GetFileName(filePath));
            }
        }
    }

    private async IAsyncEnumerable<string> ReadLinesFromLocalFileAsync(string filePath, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(filePath, Encoding.UTF8);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync();
            if (line == null)
                break;

            if (IsIrrelevantLine(line))
                continue;

            yield return line;
        }
    }

    public async IAsyncEnumerable<string> UnreadFileLinesAsync(
        EFileType fileType,
        IReaderPointerRepository readerPointerRepository,
        IFtpService ftpService,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_ftp is null)
            yield break;

        AsyncFtpClient client = null!;
        var semaphore = _semaphores.GetOrAdd((fileType, _scumServer.Id), new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            // Get pooled client safely
            client = await ftpService.GetClientAsync(_ftp, cancellationToken);

            // Fetch files from FTP
            List<FtpListItem> ftpFiles = await GetLogFiles(client, _ftp.RootFolder, fileType);

            var filteredFiles = new List<(FtpListItem File, ReaderPointer? Pointer)>();

            foreach (var file in ftpFiles)
            {
                var pointer = await readerPointerRepository.FindOneAsync(p => p.FileName == file.Name);
                if (pointer == null || pointer.LastUpdated != file.Modified || pointer.FileSize != file.Size)
                {
                    filteredFiles.Add((file, pointer));
                }
            }

            if (filteredFiles.Count == 0)
                yield break;

            string localPath = Path.Combine(GetLocalPath(), "logs");
            Directory.CreateDirectory(localPath);

            await ftpService.CopyFilesAsync(client, localPath, filteredFiles.Select(f => f.File.FullName).ToList(), cancellationToken);

            foreach (var (file, pointer) in filteredFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ReaderPointer currentPointer = pointer is not null
                    ? UpdateReaderPointer(pointer, file)
                    : BuildReaderPointer(file);

                string filePath = Path.Combine(localPath, file.Name);

                await foreach (var line in ReadLinesFromFileAsync(filePath, currentPointer, readerPointerRepository, cancellationToken))
                {
                    string cleaned = new string(line.Where(c => !char.IsControl(c) || c == '\r' || c == '\n').ToArray());
                    yield return cleaned;
                }
            }
        }
        finally
        {
            semaphore.Release();
            if (client != null)
                await ftpService.ReleaseClientAsync(client);
        }
    }

    public async IAsyncEnumerable<string> FileLinesAsync(
        EFileType fileType,
        IFtpService ftpService,
        DateTime from,
        DateTime to,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_ftp is null)
            yield break;

        AsyncFtpClient client = null!;
        var semaphore = _semaphores.GetOrAdd((fileType, _scumServer.Id), new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            client = await ftpService.GetClientAsync(_ftp, cancellationToken);

            List<FtpListItem> ftpFiles = await GetLogFiles(client, _ftp.RootFolder, from, to, fileType);

            string localPath = Path.Combine(GetLocalPath(), "logs");
            Directory.CreateDirectory(localPath);

            var filesToDownload = ftpFiles
                .Where(f =>
                {
                    var localFilePath = Path.Combine(localPath, f.Name);
                    var localFile = new FileInfo(localFilePath);
                    return !localFile.Exists || f.Size != localFile.Length;
                })
                .ToList();

            if (filesToDownload.Count > 0)
                await ftpService.CopyFilesAsync(client, localPath, filesToDownload.Select(f => f.FullName).ToList(), cancellationToken);

            foreach (var file in ftpFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var localFilePath = Path.Combine(localPath, file.Name);
                FileInfo localFile = new FileInfo(localFilePath);

                if (!localFile.Exists)
                    continue;

                await foreach (var line in ReadLinesFromLocalFileAsync(localFile.FullName, cancellationToken))
                {
                    string cleaned = new string(line.Where(c => !char.IsControl(c) || c == '\r' || c == '\n').ToArray());
                    yield return cleaned;
                }
            }
        }
        finally
        {
            semaphore.Release();
            if (client != null)
                await ftpService.ReleaseClientAsync(client);
        }
    }

    public async Task<string> DownloadRaidTimes(IFtpService ftpService)
    {
        var client = await ftpService.GetClientAsync(_ftp);
        var remotePath = $"{_ftp.RootFolder}/Saved/Config/WindowsServer/RaidTimes.json";
        string localPath = GetLocalPath();

        await ftpService.CopyFilesAsync(client, localPath, [remotePath]);
        _logger.LogDebug("Downloaded RaidTimes.json from server {Server}", _scumServer.Id);
        using var reader = new StreamReader(Path.Combine(localPath, "RaidTimes.json"));
        return await reader.ReadToEndAsync();
    }

    public async Task<string> ReadLocalRaidTimesAsync(CancellationToken token = default)
    {
        string localPath = Path.Combine(GetLocalPath(), "RaidTimes.json");
        using var reader = new StreamReader(localPath);
        return await reader.ReadToEndAsync(token);
    }

    public async Task SaveSquadList(string squadList)
    {
        string localPath = Path.Combine(GetLocalPath(), "squadlist.json");
        _logger.LogDebug("Saving SquadList from server {Server}", _scumServer.Id);
        await using var writer = new StreamWriter(localPath, false);
        await writer.WriteAsync(squadList);
    }

    public async Task<string> ReadSquadListAsync(CancellationToken token = default)
    {
        string localPath = Path.Combine(GetLocalPath(), "squadlist.json");
        using var reader = new StreamReader(localPath);
        return await reader.ReadToEndAsync(token);
    }

    public async Task SaveFlagList(string flagList)
    {
        string localPath = Path.Combine(GetLocalPath(), "flaglist.json");
        _logger.LogDebug("Saving FlagList from server {Server}", _scumServer.Id);
        await using var writer = new StreamWriter(localPath, false);
        await writer.WriteAsync(flagList);
    }

    public async Task<string> ReadFlagListAsync(CancellationToken token = default)
    {
        string localPath = Path.Combine(GetLocalPath(), "flaglist.json");
        using var reader = new StreamReader(localPath);
        return await reader.ReadToEndAsync(token);
    }
}