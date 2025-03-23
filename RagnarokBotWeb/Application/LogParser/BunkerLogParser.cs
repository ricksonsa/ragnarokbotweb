using System.Text.RegularExpressions;

namespace RagnarokBotWeb.Application.LogParser
{
    public class BunkerLogParser
    {
        public (string, bool, TimeSpan) Parse(string line)
        {
            Regex bunkerIdRegex = new Regex(@"\b([A-Z]\d)\b");
            Regex stateRegex = new Regex(@"\b(Active|Locked)\b");
            Regex activationTimeRegex = new Regex(@"(\d{2}h \d{2}m \d{2}s)");

            // Extracting data
            Match bunkerIdMatch = bunkerIdRegex.Match(line);
            Match stateMatch = stateRegex.Match(line);
            Match activationTimeMatch = activationTimeRegex.Match(line);

            // Regex to extract hours, minutes, and seconds
            var match = Regex.Match(activationTimeMatch.Value, @"(?:(\d+)h)?\s*(?:(\d+)m)?\s*(?:(\d+)s)?");

            int hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
            int minutes = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
            int seconds = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

            TimeSpan timeSpan = new(hours, minutes, seconds);

            var sector = bunkerIdMatch.Value;
            var locked = stateMatch.Value == "Locked";

            return (sector, locked, timeSpan);
        }
    }
}
