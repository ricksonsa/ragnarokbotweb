using System.Diagnostics;
using System.IO.Compression;

class Program
{
    private const string BASE_URL = "https://api.thescumbot.com:8082";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Downloading TheSCUMBot...");
        Thread.Sleep(1500);

        if (args.Length == 0)
        {
            Console.WriteLine("Error: No version argument provided.");
            return;
        }

        string version = args[0];
        string zipPath = Path.Combine(Path.GetTempPath(), "thescumbot.zip");
        string extractPath = Path.Combine(Path.GetTempPath(), "thescumbot_extracted");

        try
        {
            // 1. Download version
            await DownloadVersion(version);

            // 2. Clean old temp folder & extract
            if (Directory.Exists(extractPath))
                Directory.Delete(extractPath, true);

            Console.WriteLine("Updating TheSCUMBot...");
            ZipFile.ExtractToDirectory(zipPath, extractPath);

            // 3. Create batch file to replace files after exit
            string batFile = Path.Combine(Path.GetTempPath(), "update_thescumbot.bat");
            string targetDir = AppContext.BaseDirectory;

            File.WriteAllText(batFile, $@"
@echo off
ping 127.0.0.1 -n 3 > nul
xcopy ""{extractPath}\*"" ""{targetDir}"" /E /Y
rd /s /q ""{extractPath}""
start """" ""{Path.Combine(targetDir, "TheSCUMBot.exe")}""
del ""%~f0""
");

            // 4. Start batch and exit
            Process.Start(new ProcessStartInfo
            {
                FileName = batFile,
                CreateNoWindow = true,
                UseShellExecute = false
            });

            Console.WriteLine("Update applied. Restarting bot...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static async Task DownloadVersion(string version)
    {
        string zipPath = Path.Combine(Path.GetTempPath(), "thescumbot.zip");
        using (HttpClient client = new HttpClient())
        using (HttpResponseMessage response = await client.GetAsync($"{BASE_URL}/images/thescumbot-{version}.zip"))
        using (Stream stream = await response.Content.ReadAsStreamAsync())
        using (FileStream fileStream = new FileStream(zipPath, FileMode.Create))
        {
            await stream.CopyToAsync(fileStream);
        }
    }
}
