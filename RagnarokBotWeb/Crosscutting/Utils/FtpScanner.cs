using FluentFTP;
using System.Diagnostics;

public class FtpScanner
{
    private readonly AsyncFtpClient _client;

    public FtpScanner(string host, string username, string password)
    {
        _client = new AsyncFtpClient(host)
        {
            Credentials = new System.Net.NetworkCredential(username, password),
            Config = new FtpConfig
            {
                ConnectTimeout = 300000,
                DataConnectionConnectTimeout = 300000,
                ReadTimeout = 300000,
                DataConnectionReadTimeout = 300000
            }
        };
    }

    public async Task<List<string>> FindServerSettingsFilesAsync(string fileToFind, string startPath = "/")
    {
        List<string> foundFiles = [];

        await _client.Connect();

        await TraverseDirectoriesForDirectoryAsync(fileToFind, startPath, foundFiles);

        await _client.Disconnect();

        return foundFiles;
    }

    private async Task TraverseDirectoriesForDirectoryAsync(string dirToFind, string currentPath, List<string> foundDirectories)
    {
        foreach (var item in await _client.GetListing(currentPath))
        {
            Debug.WriteLine(item.FullName);

            if (item.Type == FtpObjectType.Directory)
            {
                if (item.Name.Equals(dirToFind, StringComparison.Ordinal))
                {
                    foundDirectories.Add(item.FullName);
                    return;
                }

                // Recurse into subdirectory
                await TraverseDirectoriesForDirectoryAsync(dirToFind, item.FullName, foundDirectories);
            }
        }
    }
}
