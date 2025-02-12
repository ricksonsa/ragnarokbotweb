using System.Text.RegularExpressions;

namespace RagnarokBotWeb.Application.LogParser
{
    public class LoginLogParser : ILogParser<(string, string, string, bool)>
    {
        public (string, string, string, bool) Parse(string line)
        {
            Match match1 = Regex.Match(line, @"(\d+):([A-Za-z]+)");

            string steamId64 = match1.Groups[1].Value;

            string name = match1.Groups[2].Value;

            Match match2 = Regex.Match(line, @"\((\d+)\)");

            string scumId = match2.Groups[1].Value;
            bool isLoggedIn = line.Contains("logged in");

            return (steamId64, scumId, name, isLoggedIn);
        }
    }
}
