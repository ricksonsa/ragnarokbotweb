using System.Diagnostics;
using System.IO.Compression;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Updating TheSCUMBot...");
        Thread.Sleep(1500);

        string zipPath = Path.Combine(Path.GetTempPath(), "thescumbot.zip");

        try
        {
            ZipFile.ExtractToDirectory(zipPath, AppContext.BaseDirectory, overwriteFiles: true);
            Thread.Sleep(500);
            Process.Start("TheSCUMBot.exe");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
