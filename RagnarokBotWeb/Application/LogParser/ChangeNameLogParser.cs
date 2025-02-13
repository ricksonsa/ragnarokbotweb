using System.Text.RegularExpressions;

namespace RagnarokBotWeb.Application.LogParser
{
    public class ChangeNameLogParser
    {
        public (string, string, string) Parse(string line)
        {
            string pattern = @"\((\d+),\s\d+\)";

            Match match = Regex.Match(line, pattern);
            var scumId = match.Groups[1].Value;

            pattern = @"\(\d+,\s*(\d+)\)";
            match = Regex.Match(line, pattern);

            var steamId64 = match.Groups[1].Value;
            var newName = line.Split("changed their name to ")[1].Replace(".", "");

            return (steamId64, scumId, newName);
        }
    }
}
