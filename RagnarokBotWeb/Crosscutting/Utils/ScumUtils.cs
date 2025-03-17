namespace RagnarokBotWeb.Crosscutting.Utils;

public static class ScumUtils
{
    public static DateTime ParseDateTime(string fileName)
    {
        return DateTime.ParseExact(fileName.Split("_")[1].Replace(".log", string.Empty), "yyyyMMddHHmmss", null);
    }
}