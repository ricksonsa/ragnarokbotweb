using FluentFTP;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Serilog;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

namespace RagnarokBotWeb.HostedServices.Base;

public class ScumFileProcessor
{
    private static readonly Func<string, string> AppDataPathFunc =
        server =>
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", server);
            Directory.CreateDirectory(path);
            return path;
        };

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ScumFileProcessor> _logger;
    private readonly ScumServer _scumServer;
    private readonly Ftp? _ftp;
    private static readonly ConcurrentDictionary<(string, long), (SemaphoreSlim, DateTime)> _semaphores = [];

    public ScumFileProcessor(ScumServer server, IUnitOfWork unitOfWork)
    {
        var loggerFactory = new LoggerFactory();
        loggerFactory.AddSerilog();
        _logger = loggerFactory.CreateLogger<ScumFileProcessor>();
        _scumServer = server ?? throw new ArgumentNullException(nameof(server));
        _ftp = server.Ftp; // Remove the ! operator to allow null
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        ClearOldSemaphores();
    }

    private static bool IsIrrelevantLine(ReadOnlySpan<char> line)
    {
        return line.IsEmpty || line.IsWhiteSpace() || line.Contains("Game version".AsSpan(), StringComparison.Ordinal);
    }

    private static bool IsExpired(DateTime createdAt) => DateTime.UtcNow - createdAt > TimeSpan.FromMinutes(10);

    private static void ClearOldSemaphores()
    {
        foreach (var item in _semaphores.ToArray()) // Convert to array to avoid collection modification issues
        {
            var (_, date) = item.Value;
            if (IsExpired(date))
                _semaphores.TryRemove(item.Key, out _);
        }
    }

    private async Task<List<FtpListItem>> GetLogFilesAsync(IFtpService ftpService, string? rootFolder, EFileType fileType, CancellationToken cancellationToken)
    {
        if (_ftp == null)
        {
            _logger.LogError("FTP configuration is null for server {ServerId}", _scumServer.Id);
            throw new InvalidOperationException($"FTP configuration is null for server {_scumServer.Id}");
        }

        if (string.IsNullOrEmpty(rootFolder))
        {
            _logger.LogError("Root folder is null or empty for server {ServerId}", _scumServer.Id);
            throw new InvalidOperationException($"Root folder is null or empty for server {_scumServer.Id}");
        }

        return await ftpService.ExecuteWithRetryAsync(_ftp, async client =>
        {
            var today = DateTime.UtcNow;
            var logPath = $"{rootFolder}/Saved/SaveFiles/Logs/";

            try
            {
                // Ensure client is connected
                if (!client.IsConnected)
                {
                    _logger.LogInformation("FTP client not connected, attempting to connect for server {ServerId}", _scumServer.Id);
                    await client.Connect(cancellationToken);
                }

                // Check if directory exists
                if (!await client.DirectoryExists(logPath, cancellationToken))
                {
                    _logger.LogWarning("Log directory does not exist: {LogPath} for server {ServerId}", logPath, _scumServer.Id);
                    return new List<FtpListItem>();
                }

                var listing = await client.GetListing(logPath, FtpListOption.Modify | FtpListOption.Size, cancellationToken);

                if (listing == null)
                {
                    _logger.LogWarning("FTP listing returned null for path {LogPath} on server {ServerId}", logPath, _scumServer.Id);
                    return new List<FtpListItem>();
                }

                var fileTypePrefix = fileType.ToString().ToLower();
                var filteredFiles = listing
                    .Where(file => file != null &&
                                 !string.IsNullOrEmpty(file.Name) &&
                                 file.Name.StartsWith(fileTypePrefix, StringComparison.OrdinalIgnoreCase) &&
                                 IsFileInValidTimeRange(file, today))
                    .OrderBy(file => file.RawModified)
                    .ToList();

                _logger.LogDebug("Found {Count} log files of type {FileType} for server {ServerId}",
                    filteredFiles.Count, fileType, _scumServer.Id);

                return filteredFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting log files of type {FileType} from path {LogPath} for server {ServerId}",
                    fileType, logPath, _scumServer.Id);
                throw;
            }
        }, cancellationToken);
    }

    private static bool IsFileInValidTimeRange(FtpListItem file, DateTime today)
    {
        try
        {
            var fileMonth = file.RawModified.Month;
            var currentMonth = today.Month;

            // Allow current month, previous month, and next month
            return fileMonth == currentMonth ||
                   fileMonth == currentMonth - 1 ||
                   fileMonth == currentMonth + 1 ||
                   (currentMonth == 1 && fileMonth == 12) || // Handle year boundary
                   (currentMonth == 12 && fileMonth == 1);
        }
        catch (Exception)
        {
            // If there's any issue with date comparison, include the file
            return true;
        }
    }

    private async Task<List<FtpListItem>> GetLogFilesWithRetryAsync(IFtpService ftpService, string? rootFolder, DateTime from, DateTime to, EFileType fileType)
    {
        // Add null checks
        if (_ftp == null)
        {
            _logger.LogError("FTP configuration is null for server {ServerId}", _scumServer.Id);
            throw new InvalidOperationException($"FTP configuration is null for server {_scumServer.Id}");
        }

        if (string.IsNullOrEmpty(rootFolder))
        {
            _logger.LogError("Root folder is null or empty for server {ServerId}", _scumServer.Id);
            throw new InvalidOperationException($"Root folder is null or empty for server {_scumServer.Id}");
        }

        return await ftpService.ExecuteWithRetryAsync(_ftp, async client =>
        {
            var logPath = $"{rootFolder}/Saved/SaveFiles/Logs/";

            try
            {
                // Ensure client is connected
                if (!client.IsConnected)
                {
                    _logger.LogInformation("FTP client not connected, attempting to connect for server {ServerId}", _scumServer.Id);
                    await client.Connect();
                }

                // Check if directory exists
                if (!await client.DirectoryExists(logPath))
                {
                    _logger.LogWarning("Log directory does not exist: {LogPath} for server {ServerId}", logPath, _scumServer.Id);
                    return new List<FtpListItem>();
                }

                var listing = await client.GetListing(logPath, FtpListOption.Modify | FtpListOption.Size);

                if (listing == null)
                {
                    _logger.LogWarning("FTP listing returned null for path {LogPath} on server {ServerId}", logPath, _scumServer.Id);
                    return new List<FtpListItem>();
                }

                var fileTypePrefix = $"{fileType.ToString().ToLower()}_";
                var filteredFiles = listing
                    .Where(file => file != null &&
                                 !string.IsNullOrEmpty(file.Name) &&
                                 file.Name.StartsWith(fileTypePrefix, StringComparison.OrdinalIgnoreCase) &&
                                 file.RawModified.Date >= from &&
                                 file.RawModified.Date <= to)
                    .OrderBy(file => file.RawModified)
                    .ToList();

                _logger.LogDebug("Found {Count} log files of type {FileType} between {From} and {To} for server {ServerId}",
                    filteredFiles.Count, fileType, from.Date, to.Date, _scumServer.Id);

                return filteredFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting log files of type {FileType} from path {LogPath} for server {ServerId}",
                    fileType, logPath, _scumServer.Id);
                throw;
            }
        });
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
            FileDate = item.Created
        };
    }

    private ReaderPointer UpdateReaderPointer(ReaderPointer pointer, FtpListItem item)
    {
        pointer.FileSize = item.Size;
        pointer.LastUpdated = item.Modified;
        return pointer;
    }

    /// <summary>
    /// Efficiently cleans a line by removing control characters in-place using Span
    /// </summary>
    private static string CleanLine(ReadOnlySpan<char> line)
    {
        Span<char> buffer = stackalloc char[line.Length];
        int writeIndex = 0;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (!char.IsControl(c) || c == '\r' || c == '\n')
            {
                buffer[writeIndex++] = c;
            }
        }

        return buffer[..writeIndex].ToString();
    }

    /// <summary>
    /// Prepare a single file for streaming by downloading it
    /// </summary>
    private async Task<(string localFilePath, ReaderPointer currentPointer)?> PrepareFileForStreamingAsync(
        FtpListItem file,
        ReaderPointer? existingPointer,
        IFtpService ftpService,
        CancellationToken cancellationToken = default)
    {
        if (_ftp == null)
        {
            _logger.LogError("FTP configuration is null, cannot prepare file for streaming");
            return null;
        }

        var (semaphore, _) = _semaphores.GetOrAdd((file.FullName, _scumServer.Id), (new SemaphoreSlim(1, 1), DateTime.Now));

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            // Download single file to local temp location
            string localPath = Path.Combine(GetLocalPath(), "temp");
            Directory.CreateDirectory(localPath);

            string localFilePath = Path.Combine(localPath, file.Name);

            await ftpService.ExecuteWithRetryAsync(_ftp, async client =>
            {
                await ftpService.CopyFilesAsync(client, localPath, [file.FullName], cancellationToken);
                return true;
            }, cancellationToken);

            // Prepare pointer
            ReaderPointer currentPointer = existingPointer is not null
                ? UpdateReaderPointer(existingPointer, file)
                : BuildReaderPointer(file);

            return (localFilePath, currentPointer);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Stream lines from a single file with minimal memory allocation
    /// </summary>
    private async IAsyncEnumerable<string> StreamLinesFromFileAsync(
        FtpListItem file,
        ReaderPointer? existingPointer,
        IFtpService ftpService,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var preparedFile = await PrepareFileForStreamingAsync(file, existingPointer, ftpService, cancellationToken);

        if (preparedFile == null)
            yield break;

        var (localFilePath, currentPointer) = preparedFile.Value;

        // Stream lines with minimal memory usage
        await foreach (var line in ReadLinesFromFileAsync(localFilePath, currentPointer, cancellationToken))
        {
            yield return line;
        }
    }

    private async IAsyncEnumerable<string> ReadLinesFromFileAsync(
        string filePath,
        ReaderPointer currentPointer,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int lineNumber = 0;
        int linesSinceLastPointerUpdate = 0;
        const int PointerUpdateInterval = 100;
        var readerPointerRepository = new ReaderPointerRepository(_unitOfWork.CreateDbContext());

        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096);
        using var reader = new StreamReader(fileStream, Encoding.UTF8);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);
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

            // Use span-based cleaning for efficiency
            var cleanedLine = CleanLine(line);
            if (!string.IsNullOrWhiteSpace(cleanedLine))
            {
                yield return cleanedLine;
            }

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
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096);
        using var reader = new StreamReader(fileStream, Encoding.UTF8);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null)
                break;

            if (IsIrrelevantLine(line))
                continue;

            var cleanedLine = CleanLine(line);
            if (!string.IsNullOrWhiteSpace(cleanedLine))
            {
                yield return cleanedLine;
            }
        }
    }

    /// <summary>
    /// True streaming implementation - processes files one by one with minimal memory usage
    /// </summary>
    public async IAsyncEnumerable<string> UnreadFileLinesAsync(
        EFileType fileType,
        IFtpService ftpService,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_ftp is null)
        {
            _logger.LogWarning("FTP configuration is null for server {ServerId}, cannot process files", _scumServer.Id);
            yield break;
        }

        List<FtpListItem> ftpFiles;
        try
        {
            ftpFiles = await GetLogFilesAsync(ftpService, _ftp.RootFolder, fileType, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file list for server {ServerId}", _scumServer.Id);
            if (IsConnectionError(ex))
            {
                await ftpService.ClearPoolForServerAsync(_ftp);
            }
            throw;
        }

        var readerPointerRepository = new ReaderPointerRepository(_unitOfWork.CreateDbContext());

        // Process files sequentially to minimize memory usage
        foreach (var file in ftpFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ReaderPointer? pointer = null;
            bool shouldProcessFile = false;

            // Check if we should process this file (outside yield context)
            try
            {
                pointer = await readerPointerRepository.FindOneAsync(p => p.FileName == file.Name);
                shouldProcessFile = pointer == null || pointer.LastUpdated != file.Modified || pointer.FileSize != file.Size;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking pointer for file {FileName}", file.Name);
                continue;
            }

            if (!shouldProcessFile)
                continue;

            // Now we can safely stream lines (no try-catch around yield)
            await foreach (var line in StreamLinesWithErrorHandlingAsync(file, pointer, ftpService, cancellationToken))
            {
                yield return line;
            }
        }
    }

    /// <summary>
    /// Wrapper method that handles errors without yield return
    /// </summary>
    private async IAsyncEnumerable<string> StreamLinesWithErrorHandlingAsync(
        FtpListItem file,
        ReaderPointer? pointer,
        IFtpService ftpService,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerable<string>? lineStream = null;

        try
        {
            lineStream = StreamLinesFromFileAsync(file, pointer, ftpService, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing to stream file {FileName} for server {ServerId}", file.Name, _scumServer.Id);

            if (IsConnectionError(ex))
            {
                if (_ftp != null)
                    await ftpService.ClearPoolForServerAsync(_ftp);
            }
            // Return empty stream on error
            yield break;
        }

        if (lineStream != null)
        {
            await foreach (var line in lineStream)
            {
                yield return line;
            }
        }
    }

    /// <summary>
    /// True streaming implementation for date-range queries
    /// </summary>
    public async IAsyncEnumerable<string> FileLinesAsync(
        EFileType fileType,
        IFtpService ftpService,
        DateTime from,
        DateTime to,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_ftp is null)
        {
            _logger.LogWarning("FTP configuration is null for server {ServerId}, cannot process files", _scumServer.Id);
            yield break;
        }

        List<FtpListItem> ftpFiles;
        try
        {
            ftpFiles = await GetLogFilesWithRetryAsync(ftpService, _ftp.RootFolder, from, to, fileType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file list for server {ServerId}", _scumServer.Id);
            if (IsConnectionError(ex))
            {
                await ftpService.ClearPoolForServerAsync(_ftp);
            }
            throw;
        }

        string localPath = Path.Combine(GetLocalPath(), "logs");
        Directory.CreateDirectory(localPath);

        // Process files sequentially
        foreach (var file in ftpFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Prepare file (download if needed) outside yield context
            string? localFilePath = null;
            try
            {
                localFilePath = await PrepareLocalFileAsync(file, localPath, ftpService, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing file {FileName} for server {ServerId}", file.Name, _scumServer.Id);

                if (IsConnectionError(ex))
                {
                    await ftpService.ClearPoolForServerAsync(_ftp);
                }
                // Continue with next file
                continue;
            }

            if (localFilePath != null)
            {
                // Stream lines from local file (no try-catch around yield)
                await foreach (var line in ReadLinesFromLocalFileAsync(localFilePath, cancellationToken))
                {
                    yield return line;
                }
            }
        }
    }

    /// <summary>
    /// Prepare local file for streaming (download if needed)
    /// </summary>
    private async Task<string?> PrepareLocalFileAsync(
        FtpListItem file,
        string localPath,
        IFtpService ftpService,
        CancellationToken cancellationToken)
    {
        if (_ftp == null)
        {
            _logger.LogError("FTP configuration is null, cannot prepare local file");
            return null;
        }

        string localFilePath = Path.Combine(localPath, file.Name);
        FileInfo localFile = new(localFilePath);

        // Download only if needed
        if (!localFile.Exists || file.Size != localFile.Length)
        {
            await ftpService.ExecuteWithRetryAsync(_ftp, async client =>
            {
                await ftpService.CopyFilesAsync(client, localPath, [file.FullName], cancellationToken);
                return true;
            }, cancellationToken);
        }

        return localFile.Exists ? localFilePath : null;
    }

    public async Task<string> DownloadRaidTimes(IFtpService ftpService)
    {
        if (_ftp == null)
        {
            _logger.LogError("FTP configuration is null for server {ServerId}, cannot download RaidTimes", _scumServer.Id);
            throw new InvalidOperationException($"FTP configuration is null for server {_scumServer.Id}");
        }

        var remotePath = $"{_ftp.RootFolder}/Saved/Config/WindowsServer/RaidTimes.json";
        string localPath = GetLocalPath();

        try
        {
            await ftpService.ExecuteWithRetryAsync(_ftp, async client =>
            {
                await ftpService.CopyFilesAsync(client, localPath, [remotePath]);
                return true; // dummy return
            });

            _logger.LogDebug("Downloaded RaidTimes.json from server {Server}", _scumServer.Id);
            using var reader = new StreamReader(Path.Combine(localPath, "RaidTimes.json"));
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading RaidTimes for server {ServerId}", _scumServer.Id);

            if (IsConnectionError(ex))
            {
                _logger.LogWarning("Clearing FTP pool for server {ServerId} due to connection error", _scumServer.Id);
                await ftpService.ClearPoolForServerAsync(_ftp);
            }

            throw;
        }
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

    private static bool IsConnectionError(Exception ex)
    {
        return ex is TimeoutException ||
               ex is System.Net.Sockets.SocketException ||
               ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("server", StringComparison.OrdinalIgnoreCase);
    }

    public async Task ClearFtpPoolAsync(IFtpService ftpService)
    {
        if (_ftp != null)
        {
            await ftpService.ClearPoolForServerAsync(_ftp);
            _logger.LogInformation("Manually cleared FTP pool for server {ServerId}", _scumServer.Id);
        }
        else
        {
            _logger.LogWarning("Cannot clear FTP pool - FTP configuration is null for server {ServerId}", _scumServer.Id);
        }
    }
}