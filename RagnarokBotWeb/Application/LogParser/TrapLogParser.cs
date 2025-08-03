using RagnarokBotWeb.Application.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RagnarokBotWeb.Application.LogParser
{
    public class TrapLogParser
    {
        public static TrapLog? Parse(string logLine)
        {
            var pattern = @"User:\s(?<user>.+?)\s\((?<scumId>\d+),\s(?<steamId>\d+)\)\. Trap name:\s(?<trapName>.+?)\. Location:\sX=(?<x>[\d.]+)\sY=(?<y>[\d.]+)\sZ=(?<z>[\d.]+)";
            var match = Regex.Match(logLine, pattern);

            if (!match.Success)
                return null;

            return new TrapLog
            {
                User = match.Groups["user"].Value,
                ScumId = int.Parse(match.Groups["scumId"].Value),
                SteamId = match.Groups["steamId"].Value,
                TrapName = match.Groups["trapName"].Value,
                X = float.Parse(match.Groups["x"].Value, CultureInfo.InvariantCulture),
                Y = float.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture),
                Z = float.Parse(match.Groups["z"].Value, CultureInfo.InvariantCulture)
            };
        }
    }
}
