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
    private static readonly ConcurrentDictionary<(long, string), DateTime> _semaphores = [];

    public ScumFileProcessor(ScumServer server, IUnitOfWork unitOfWork)
    {
        var loggerFactory = new LoggerFactory();
        loggerFactory.AddSerilog();
        _logger = loggerFactory.CreateLogger<ScumFileProcessor>();
        _scumServer = server ?? throw new ArgumentNullException(nameof(server));
        _ftp = server.Ftp;
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    private static bool IsIrrelevantLine(ReadOnlySpan<char> line)
    {
        if (line.IsEmpty || line.IsWhiteSpace())
            return true;

        return line.ToString().Contains("Game version", StringComparison.Ordinal);
    }

    private static bool IsExpired(DateTime createdAt) => DateTime.UtcNow - createdAt > TimeSpan.FromMinutes(10);

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

        var client = await ftpService.GetClientAsync(_ftp, cancellationToken);
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

            var fileTypePrefix = fileType.ToString().ToLower();

            var fileNames = await client.GetNameListing(logPath, cancellationToken);

            var matchingFiles = new List<FtpListItem>();
            fileNames = fileNames
                .Select(name => Path.GetFileName(name))
                .Where(name => name.StartsWith(fileTypePrefix, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f =>
                {
                    string fileName = Path.GetFileNameWithoutExtension(f);
                    string datePart = fileName.Replace($"{fileTypePrefix}_", "");
                    if (DateTime.TryParseExact(datePart, "yyyyMMddHHmmss",
                                               null,
                                               System.Globalization.DateTimeStyles.None,
                                               out var dt))
                    {
                        return dt;
                    }
                    return DateTime.MinValue;
                })
                .Take(10)
                .ToArray();

            foreach (var fileName in fileNames)
            {
                try
                {
                    var fileInfo = await client.GetObjectInfo($"{logPath}{fileName}", dateModified: true);
                    if (fileInfo != null && fileInfo.Type == FtpObjectType.File)
                    {
                        var listItem = new FtpListItem
                        {
                            Name = fileName,
                            FullName = $"{logPath}/{fileName}",
                            Type = FtpObjectType.File,
                            Size = fileInfo.Size,
                            Modified = fileInfo.Modified,
                            RawModified = fileInfo.RawModified
                        };
                        matchingFiles.Add(listItem);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not get info for file {FileName}", fileName);
                    continue;
                }
            }

            return matchingFiles.OrderBy(f => f.RawModified).ToList();
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogError(ex, "Error getting log files of type {FileType} from path {LogPath} for server {ServerId}",
                fileType, logPath, _scumServer.Id);
            throw new InvalidOperationException("FTP client was disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting log files of type {FileType} from path {LogPath} for server {ServerId}",
                fileType, logPath, _scumServer.Id);
            throw;
        }
    }

    private static bool IsFileInValidTimeRange(FtpListItem file, DateTime today, int daysBack = 10)
    {
        try
        {
            var modified = file.RawModified != DateTime.MinValue
                ? file.RawModified
                : file.Modified;

            var cutoffDate = today.AddDays(-daysBack);

            return modified >= cutoffDate && modified <= today;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private async Task<List<FtpListItem>> GetLogFilesWithRetryAsync(
        IFtpService ftpService,
        string? rootFolder,
        DateTime from,
        DateTime to,
        EFileType fileType,
        CancellationToken cancellationToken = default)
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

        return await ftpService.ExecuteAsync(_ftp, async client =>
        {
            var logPath = $"{rootFolder}/Saved/SaveFiles/Logs/";

            try
            {
                if (!client.IsConnected)
                {
                    _logger.LogInformation("FTP client not connected, attempting to connect for server {ServerId}", _scumServer.Id);
                    await client.Connect(cancellationToken);
                }

                var now = DateTime.UtcNow;
                var fileTypePrefix = fileType.ToString().ToLower();
                var fileNames = await client.GetNameListing(logPath, cancellationToken);

                var matchingFiles = new List<FtpListItem>();

                fileNames = fileNames
                    .Select(name => Path.GetFileName(name))
                    .Where(name => name.StartsWith(fileTypePrefix, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(f =>
                    {
                        string fileName = Path.GetFileNameWithoutExtension(f);
                        string datePart = fileName.Replace($"{fileTypePrefix}_", "");
                        if (DateTime.TryParseExact(datePart, "yyyyMMddHHmmss",
                                                   null,
                                                   System.Globalization.DateTimeStyles.None,
                                                   out var dt))
                        {
                            return dt;
                        }
                        return DateTime.MinValue;
                    })
                    .ToArray();

                foreach (var fileName in fileNames)
                {
                    try
                    {
                        var fileInfo = await client.GetObjectInfo($"{logPath}{fileName}");

                        if (fileInfo != null &&
                            fileInfo.Type == FtpObjectType.File &&
                            fileInfo.RawModified.Date >= from.Date &&
                            fileInfo.RawModified.Date <= to.Date)
                        {
                            var listItem = new FtpListItem
                            {
                                Name = fileName,
                                FullName = $"{logPath}{fileName}",
                                Type = FtpObjectType.File,
                                Size = fileInfo.Size,
                                Modified = fileInfo.Modified,
                                RawModified = fileInfo.RawModified
                            };

                            matchingFiles.Add(listItem);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Could not get info for file {FileName}", fileName);
                        continue;
                    }
                }

                var ordered = matchingFiles.OrderBy(f => f.RawModified).ToList();

                _logger.LogDebug("Found {Count} log files of type {FileType} between {From} and {To} for server {ServerId}",
                    ordered.Count, fileType, from.Date, to.Date, _scumServer.Id);

                return ordered;
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

        // Download single file to local temp location
        string localPath = Path.Combine(GetLocalPath(), "temp");
        Directory.CreateDirectory(localPath);

        string localFilePath = Path.Combine(localPath, file.Name);

        await ftpService.ExecuteAsync(_ftp, async client =>
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

        await foreach (var line in ReadLinesFromFileAsync(localFilePath, currentPointer, cancellationToken))
        {
            yield return line;
        }
    }

    /// <summary>
    /// Modified to work with already downloaded local files
    /// </summary>
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

            // Use span-based cleaning for efficiency
            var cleanedLine = CleanLine(line);

            if (IsIrrelevantLine(cleanedLine))
            {
                lineNumber++;
                continue;
            }

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
    /// Now with batch downloading for better performance
    /// </summary>
    public async IAsyncEnumerable<string> UnreadFileLinesAsync(
        EFileType fileType,
        IFtpService ftpService,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_semaphores.ContainsKey((_scumServer.Id, fileType.ToString()))) yield break;
        _semaphores.TryAdd((_scumServer.Id, fileType.ToString()), DateTime.UtcNow);

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
            yield break;
        }

        var readerPointerRepository = new ReaderPointerRepository(_unitOfWork.CreateDbContext());
        var filesToProcess = new List<(FtpListItem file, ReaderPointer? pointer)>();

        try
        {
            // First, determine which files need processing and get their pointers
            foreach (var file in ftpFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ReaderPointer? pointer = null;
                bool shouldProcessFile = false;

                try
                {
                    pointer = await readerPointerRepository.FindOneAsync(p => p.FileName == file.Name && p.ScumServer.Id == _scumServer.Id);
                    shouldProcessFile = pointer == null || pointer.LastUpdated != file.Modified || pointer.FileSize != file.Size;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking pointer for file {FileName}", file.Name);
                    continue;
                }

                if (shouldProcessFile)
                {
                    filesToProcess.Add((file, pointer));
                }
            }

            if (!filesToProcess.Any())
            {
                _logger.LogDebug("No files need processing for server {ServerId}", _scumServer.Id);
                yield break;
            }

            // Download all files at once
            var localFilesPaths = await BatchDownloadFilesAsync(filesToProcess.Select(x => x.file).ToList(), ftpService, cancellationToken);

            // Process each downloaded file
            foreach (var (fileItem, pointer) in filesToProcess)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!localFilesPaths.TryGetValue(fileItem.Name, out var localFilePath) ||
                    !File.Exists(localFilePath))
                {
                    _logger.LogWarning("File {FileName} was not downloaded successfully", fileItem.Name);
                    continue;
                }

                var currentPointer = pointer is not null
                    ? UpdateReaderPointer(pointer, fileItem)
                    : BuildReaderPointer(fileItem);

                await foreach (var line in ReadLinesFromFileAsync(localFilePath, currentPointer, cancellationToken))
                {
                    yield return line;
                }
            }
        }
        finally
        {
            _semaphores.Remove((_scumServer.Id, fileType.ToString()), out var value);
        }
    }

    /// <summary>
    /// Download all files at once for better performance
    /// </summary>
    private async Task<Dictionary<string, string>> BatchDownloadFilesAsync(
        List<FtpListItem> files,
        IFtpService ftpService,
        CancellationToken cancellationToken = default)
    {
        if (_ftp == null)
        {
            _logger.LogError("FTP configuration is null, cannot batch download files");
            return new Dictionary<string, string>();
        }

        var result = new Dictionary<string, string>();

        if (!files.Any())
        {
            return result;
        }

        // Create temp directory for this batch
        string localPath = Path.Combine(GetLocalPath(), "temp");
        Directory.CreateDirectory(localPath);

        try
        {
            _logger.LogDebug("Starting batch download of {Count} files for server {ServerId}", files.Count, _scumServer.Id);

            await ftpService.ExecuteAsync(_ftp, async client =>
            {
                // Prepare list of remote file paths for batch download
                var remoteFilePaths = files.Select(f => f.FullName).ToList();

                _logger.LogDebug("Downloading files: {Files}", string.Join(", ", files.Select(f => f.Name)));

                // Use the existing CopyFilesAsync method which handles batch downloads
                await ftpService.CopyFilesAsync(client, localPath, remoteFilePaths, cancellationToken);

                // Build result dictionary with local file paths
                foreach (var file in files)
                {
                    var localFilePath = Path.Combine(localPath, file.Name);
                    if (File.Exists(localFilePath))
                    {
                        result[file.Name] = localFilePath;
                        _logger.LogDebug("Successfully downloaded {FileName} to {LocalPath}", file.Name, localFilePath);
                    }
                    else
                    {
                        _logger.LogDebug("File {FileName} was not found after download", file.Name);
                    }
                }

                return true;
            }, cancellationToken);

            _logger.LogDebug("Completed batch download: {SuccessCount}/{TotalCount} files for server {ServerId}",
                result.Count, files.Count, _scumServer.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch download for server {ServerId}", _scumServer.Id);

            if (IsConnectionError(ex))
            {
                await ftpService.ClearPoolForServerAsync(_ftp);
            }

            // Return partial results if any files were downloaded before the error
            return result;
        }

        return result;
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

        foreach (var file in ftpFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

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
                continue;
            }

            if (localFilePath != null)
            {
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
            await ftpService.ExecuteAsync(_ftp, async client =>
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
            await ftpService.ExecuteAsync(_ftp, async client =>
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