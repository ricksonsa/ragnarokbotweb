using System.Text.RegularExpressions;

namespace RagnarokBotWeb.Application.LogParser
{
    public class LoginLogParser
    {
        // steamId64, scumId, playerName, isLogin
        public (string, string, string, bool) Parse(string line)
        {
            string pattern = @"(\d{17}):([a-zA-Z]+)\((\d+)\)";

            Match match = Regex.Match(line, pattern);
            string steamId = match.Groups[1].Value;
            string name = match.Groups[2].Value;
            string scumId = match.Groups[3].Value;
            bool isLoggedIn = line.Contains("logged in");

            return (steamId, scumId, name, isLoggedIn);
        }
    }
}
