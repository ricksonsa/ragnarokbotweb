using RagnarokBotWeb.Application.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RagnarokBotWeb.Application.LogParser
{
    public class TrapLogParser
    {
        public static TrapLog? Parse(string logLine)
        {
            // Regex pattern to parse the log line
            string pattern = @"^(?<timestamp>\d{4}\.\d{2}\.\d{2}-\d{2}\.\d{2}\.\d{2}): \[LogTrap\] (?<status>\w+)\. User: (?<username>[^(]+?) \((?<scumId>\d+), (?<steamId>\d+)\)\. Trap name: (?<trapName>.*?)\. Location: X=(?<x>-?\d+(?:\.\d+)?) Y=(?<y>-?\d+(?:\.\d+)?) Z=(?<z>-?\d+(?:\.\d+)?)$";

            Regex regex = new Regex(pattern);
            Match match = regex.Match(logLine);

            if (!match.Success)
                return null;

            try
            {
                return new TrapLog
                {
                    User = match.Groups["username"].Value.Trim(),
                    ScumId = int.Parse(match.Groups["scumId"].Value),
                    SteamId = match.Groups["steamId"].Value,
                    TrapName = match.Groups["trapName"].Value,
                    X = double.Parse(match.Groups["x"].Value, CultureInfo.InvariantCulture),
                    Y = double.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture),
                    Z = double.Parse(match.Groups["z"].Value, CultureInfo.InvariantCulture)
                };
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
