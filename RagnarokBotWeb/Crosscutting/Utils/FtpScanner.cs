using FluentFTP;
using System.Diagnostics;

public class FtpScanner
{
    private readonly AsyncFtpClient _client;

    public FtpScanner(string host, int port, string username, string password)
    {
        _client = new AsyncFtpClient(host)
        {
            Port = port,
            Credentials = new System.Net.NetworkCredential(username, password),
            Config = new FtpConfig
            {
                ConnectTimeout = 30000,    // Reduced from 300000
                DataConnectionConnectTimeout = 30000,
                ReadTimeout = 30000,
                DataConnectionReadTimeout = 30000,
                RetryAttempts = 3
            }
        };
    }

    public async Task<List<string>> FindFilesAsync(string fileName, string startPath = "/", CancellationToken cancellationToken = default)
    {
        var foundFiles = new List<string>();

        try
        {
            await _client.Connect(cancellationToken);
            await SearchForFilesAsync(fileName, startPath, foundFiles, cancellationToken);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during FTP scan: {ex.Message}");
            throw;
        }
        finally
        {
            if (_client.IsConnected)
            {
                await _client.Disconnect(cancellationToken);
            }
        }

        return foundFiles;
    }

    private async Task SearchForFilesAsync(string fileName, string currentPath, List<string> foundFiles, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var items = await _client.GetListing(currentPath, cancellationToken);

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Debug.WriteLine($"Scanning: {item.FullName}");

                if (item.Type == FtpObjectType.File)
                {
                    // Check if this is the file we're looking for
                    if (item.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        foundFiles.Add(item.FullName);
                        Debug.WriteLine($"Found file: {item.FullName}");
                    }
                }
                else if (item.Type == FtpObjectType.Directory && !IsSystemDirectory(item.Name))
                {
                    // Recursively search subdirectories
                    await SearchForFilesAsync(fileName, item.FullName, foundFiles, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error accessing path '{currentPath}': {ex.Message}");
            // Continue searching other directories instead of failing completely
        }
    }

    // Optional: Find first occurrence only (more efficient if you only need one)
    public async Task<string?> FindFirstFileAsync(string fileName, string startPath = "/", CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.Connect(cancellationToken);
            return await SearchForFirstFileAsync(fileName, startPath, cancellationToken);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during FTP scan: {ex.Message}");
            throw;
        }
        finally
        {
            if (_client.IsConnected)
            {
                await _client.Disconnect(cancellationToken);
            }
        }
    }

    private async Task<string?> SearchForFirstFileAsync(string fileName, string currentPath, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var items = await _client.GetListing(currentPath, cancellationToken);

            // First, check files in current directory
            foreach (var item in items.Where(i => i.Type == FtpObjectType.File))
            {
                if (item.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"Found file: {item.FullName}");
                    return item.FullName;
                }
            }

            // Then recurse into subdirectories
            foreach (var item in items.Where(i => i.Type == FtpObjectType.Directory && !IsSystemDirectory(i.Name)))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await SearchForFirstFileAsync(fileName, item.FullName, cancellationToken);
                if (result != null)
                {
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error accessing path '{currentPath}': {ex.Message}");
        }

        return null;
    }

    private static bool IsSystemDirectory(string dirName)
    {
        // Skip common system directories that might cause issues
        return dirName == "." || dirName == ".." || dirName.StartsWith("$");
    }

    // Find first directory by name
    public async Task<string?> FindFirstDirectoryAsync(string directoryName, string startPath = "/", CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.Connect(cancellationToken);
            return await SearchForFirstDirectoryAsync(directoryName, startPath, cancellationToken);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during FTP directory scan: {ex.Message}");
            throw;
        }
        finally
        {
            if (_client.IsConnected)
            {
                await _client.Disconnect(cancellationToken);
            }
        }
    }

    private async Task<string?> SearchForFirstDirectoryAsync(string directoryName, string currentPath, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var items = await _client.GetListing(currentPath, cancellationToken);

            // First, check directories in current path
            foreach (var item in items.Where(i => i.Type == FtpObjectType.Directory && !IsSystemDirectory(i.Name)))
            {
                Debug.WriteLine($"Found Scanned: {item.FullName}");
                if (item.Name.Equals(directoryName, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"Found directory: {item.FullName}");
                    return item.FullName;
                }
            }

            // Then recurse into subdirectories
            foreach (var item in items.Where(i => i.Type == FtpObjectType.Directory && !IsSystemDirectory(i.Name)))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await SearchForFirstDirectoryAsync(directoryName, item.FullName, cancellationToken);
                if (result != null)
                {
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error accessing path '{currentPath}': {ex.Message}");
        }

        return null;
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}