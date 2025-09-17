using System.Text.RegularExpressions;

namespace RagnarokBotWeb.Application.LogParser
{
    public class LoginLogParser
    {
        public (DateTime Date, string IpAddress, string SteamId, string PlayerName, string ScumId, bool IsLoggedIn, double? X, double? Y, double? Z) Parse(string line)
        {
            string pattern =
                @"^(?<date>\d{4}\.\d{2}\.\d{2}-\d{2}\.\d{2}\.\d{2}): '\s*(?<ip>\d{1,3}(?:\.\d{1,3}){3})\s+(?<steamId>\d{17}):(?<player>.+?)\((?<scumId>\d+)\)'\s+(?<status>logged in|logged out)\s+at:\s+(?:X=(?<x>[-+]?\d*\.?\d+)\s+Y=(?<y>[-+]?\d*\.?\d+)\s+Z=(?<z>[-+]?\d*\.?\d+))\s*(?:\((?<drone>as drone)\))?$";

            var match = Regex.Match(line, pattern);
            if (!match.Success)
                throw new FormatException($"Log line not in expected format: {line}");

            var date = DateTime.ParseExact(match.Groups["date"].Value, "yyyy.MM.dd-HH.mm.ss", null);
            var ip = match.Groups["ip"].Value;
            var steamId = match.Groups["steamId"].Value;
            var player = match.Groups["player"].Value.Trim();
            var scumId = match.Groups["scumId"].Value;
            var status = match.Groups["status"].Value == "logged in";

            double? x = null, y = null, z = null;
            if (match.Groups["x"].Success)
            {
                x = double.Parse(match.Groups["x"].Value, System.Globalization.CultureInfo.InvariantCulture);
                y = double.Parse(match.Groups["y"].Value, System.Globalization.CultureInfo.InvariantCulture);
                z = double.Parse(match.Groups["z"].Value, System.Globalization.CultureInfo.InvariantCulture);
            }

            bool isDrone = match.Groups["drone"].Success;

            return (date, ip, steamId, player, scumId, status, x, y, z);
        }
    }
}
