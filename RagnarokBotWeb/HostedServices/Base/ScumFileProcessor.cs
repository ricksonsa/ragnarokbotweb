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
    private static readonly ConcurrentDictionary<(long, string), DateTime> Semaphores = [];

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

    private async Task<List<FtpListItem>> GetLogFilesAsync(IFtpService ftpService, string? rootFolder,
        EFileType fileType, CancellationToken cancellationToken)
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
        var logPath = $"{rootFolder}/Saved/SaveFiles/Logs/";

        try
        {
            // Ensure client is connected
            if (!client.IsConnected)
            {
                _logger.LogInformation("FTP client not connected, attempting to connect for server {ServerId}",
                    _scumServer.Id);
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

    private async Task<List<FtpListItem>> GetLogFilesWithDateRangeAsync(
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
                    _logger.LogInformation("FTP client not connected, attempting to connect for server {ServerId}",
                        _scumServer.Id);
                    await client.Connect(cancellationToken);
                }

                var fileTypePrefix = fileType.ToString().ToLower();
                var fileNames = await client.GetNameListing(logPath, cancellationToken);

                var matchingFiles = new List<FtpListItem>();

                fileNames = fileNames
                    .Select(Path.GetFileName)
                    .Where(name => name != null && name.StartsWith(fileTypePrefix, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(f =>
                    {
                        string? fileName = Path.GetFileNameWithoutExtension(f);
                        string? datePart = fileName?.Replace($"{fileTypePrefix}_", "");
                        if (DateTime.TryParseExact(datePart, "yyyyMMddHHmmss",
                                null,
                                System.Globalization.DateTimeStyles.None,
                                out var dt))
                        {
                            return dt;
                        }

                        return DateTime.MinValue;
                    })
                    .Take(5)
                    .ToArray();

                foreach (var fileName in fileNames)
                {
                    try
                    {
                        var fileInfo = await client.GetObjectInfo($"{logPath}{fileName}", token: cancellationToken);

                        if (fileInfo is not { Type: FtpObjectType.File } ||
                            fileInfo.RawModified.Date < from.Date ||
                            fileInfo.RawModified.Date > to.Date) continue;

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
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Could not get info for file {FileName}", fileName);
                    }
                }

                var ordered = matchingFiles.OrderBy(f => f.RawModified).ToList();

                _logger.LogDebug(
                    "Found {Count} log files of type {FileType} between {From} and {To} for server {ServerId}",
                    ordered.Count, fileType, from.Date, to.Date, _scumServer.Id);

                return ordered;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting log files of type {FileType} from path {LogPath} for server {ServerId}",
                    fileType, logPath, _scumServer.Id);
                throw;
            }
        }, cancellationToken);
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

        foreach (var c in line)
        {
            if (!char.IsControl(c) || c == '\r' || c == '\n')
            {
                buffer[writeIndex++] = c;
            }
        }

        return buffer[..writeIndex].ToString();
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
        const int pointerUpdateInterval = 100;
        var readerPointerRepository = new ReaderPointerRepository(_unitOfWork.CreateDbContext());

        await using var fileStream =
            new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096);
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

            if (!string.IsNullOrWhiteSpace(cleanedLine)) yield return cleanedLine;

            lineNumber++;
            linesSinceLastPointerUpdate++;

            if (linesSinceLastPointerUpdate < pointerUpdateInterval) continue;
            currentPointer.LineNumber = lineNumber;
            try
            {
                await readerPointerRepository.CreateOrUpdateAsync(currentPointer);
                await readerPointerRepository.SaveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating reader pointer during file read {FileName}",
                    Path.GetFileName(filePath));
            }

            linesSinceLastPointerUpdate = 0;
        }

        if (lineNumber == currentPointer.LineNumber) yield break;

        currentPointer.LineNumber = lineNumber;
        try
        {
            await readerPointerRepository.CreateOrUpdateAsync(currentPointer);
            await readerPointerRepository.SaveAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reader pointer after file read {FileName}",
                Path.GetFileName(filePath));
        }
    }

    private static async IAsyncEnumerable<string> ReadLinesFromLocalFileAsync(string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var fileStream =
            new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096);
        using var reader = new StreamReader(fileStream, Encoding.UTF8);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null)
                break;

            var cleanedLine = CleanLine(line);

            if (IsIrrelevantLine(cleanedLine))
                continue;

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
        if (Semaphores.ContainsKey((_scumServer.Id, fileType.ToString()))) yield break;
        Semaphores.TryAdd((_scumServer.Id, fileType.ToString()), DateTime.UtcNow);

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

                ReaderPointer? pointer;
                bool shouldProcessFile;

                try
                {
                    pointer = await readerPointerRepository.FindOneAsync(p =>
                        p.FileName == file.Name && p.ScumServer.Id == _scumServer.Id);
                    shouldProcessFile = pointer == null || pointer.LastUpdated != file.Modified ||
                                        pointer.FileSize != file.Size;
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
            var localFilesPaths = await BatchDownloadFilesAsync(filesToProcess.Select(x => x.file).ToList(), ftpService,
                cancellationToken);

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
            Semaphores.Remove((_scumServer.Id, fileType.ToString()), out _);
        }
    }

    /// <summary>
    /// Download all files at once for better performance
    /// </summary>
    private async Task<Dictionary<string, string>> BatchDownloadFilesAsync(
        List<FtpListItem>? files,
        IFtpService ftpService,
        CancellationToken cancellationToken = default)
    {
        if (_ftp == null)
        {
            _logger.LogError("FTP configuration is null, cannot batch download files");
            return new Dictionary<string, string>();
        }

        var result = new Dictionary<string, string>();

        if (files is { Count: 0 }) return result;

        // Create temp directory for this batch
        string localPath = Path.Combine(GetLocalPath(), "temp");
        Directory.CreateDirectory(localPath);

        try
        {
            _logger.LogDebug("Starting batch download of {Count} files for server {ServerId}", files!.Count,
                _scumServer.Id);

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
                await ftpService.ClearPoolForServerAsync(_ftp);

            // Return partial results if any files were downloaded before the error
            return result;
        }

        return result;
    }

    /// <summary>
    /// True streaming implementation for date-range queries (with batch downloading)
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
            ftpFiles = await GetLogFilesWithDateRangeAsync(ftpService, _ftp.RootFolder, from, to, fileType,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file list for server {ServerId}", _scumServer.Id);
            
            if (IsConnectionError(ex))
                await ftpService.ClearPoolForServerAsync(_ftp);

            throw;
        }

        if (!ftpFiles.Any())
        {
            _logger.LogDebug("No files found for type {FileType} in range {From} - {To} for server {ServerId}",
                fileType, from, to, _scumServer.Id);
            yield break;
        }

        // Prepare directory
        string localPath = Path.Combine(GetLocalPath(), "logs");
        Directory.CreateDirectory(localPath);

        Dictionary<string, string> localFiles;
        try
        {
            localFiles = await BatchDownloadFilesAsync(ftpFiles, ftpService, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch downloading files for server {ServerId}", _scumServer.Id);
            
            if (IsConnectionError(ex))
                await ftpService.ClearPoolForServerAsync(_ftp);
            
            throw;
        }

        foreach (var file in ftpFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!localFiles.TryGetValue(file.Name, out var localFilePath) || !File.Exists(localFilePath))
            {
                _logger.LogWarning("File {FileName} not downloaded successfully for server {ServerId}", file.Name,
                    _scumServer.Id);
                continue;
            }

            await foreach (var line in ReadLinesFromLocalFileAsync(localFilePath, cancellationToken))
            {
                yield return line;
            }
        }
    }

    public async Task<string> DownloadRaidTimes(IFtpService ftpService)
    {
        if (_ftp == null)
        {
            _logger.LogError("FTP configuration is null for server {ServerId}, cannot download RaidTimes",
                _scumServer.Id);
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
}