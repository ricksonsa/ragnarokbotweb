using System.Text.RegularExpressions;

namespace RagnarokBotWeb.Application.LogParser
{
    public class LoginLogParser
    {
        public (string, string, string, bool) Parse(string line)
        {
            string pattern = @":([a-zA-Z]+)\((\d+)\)";

            Match match = Regex.Match(line, pattern);

            string name = match.Groups[1].Value;
            string scumId = match.Groups[2].Value;

            Match match1 = Regex.Match(line, @"(\d+):([A-Za-z]+)");
            string steamId64 = match1.Groups[1].Value;
            bool isLoggedIn = line.Contains("logged in");

            return (steamId64, scumId, name, isLoggedIn);
        }
    }
}
