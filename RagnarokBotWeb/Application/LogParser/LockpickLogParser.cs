using RagnarokBotWeb.Domain.Entities;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RagnarokBotWeb.Application.LogParser
{
    public class LockpickLogParser
    {

        private readonly ScumServer _scumServer;

        public LockpickLogParser(ScumServer scumServer)
        {
            _scumServer = scumServer;
        }

        public Lockpick? Parse(string line)
        {
            string pattern = @"User:\s*(.*?)\s*\((\d+),\s*(\d+)\)";

            Match match = Regex.Match(line, pattern);

            if (match.Success)
            {
                var successSection = line.Split("Success: ")[1];
                successSection = successSection.Substring(0, successSection.IndexOf("."));

                var attemptSection = line.Split("Failed attempts: ")[1];
                attemptSection = attemptSection.Substring(0, attemptSection.IndexOf("."));

                var lockTypeSection = line.Split("Lock type: ")[1];
                lockTypeSection = lockTypeSection.Substring(0, lockTypeSection.IndexOf("."));

                var dateSection = line.Split(":")[0];

                string name = match.Groups[1].Value;
                long scumId = long.Parse(match.Groups[2].Value);
                string steamId64 = match.Groups[3].Value;
                var lockType = lockTypeSection;
                string format = "yyyy.MM.dd-HH.mm.ss";
                var date = DateTime.ParseExact(dateSection, format, CultureInfo.InvariantCulture);
                bool succes = successSection == "Yes";
                int attempts = int.Parse(attemptSection);

                return new()
                {
                    LockType = lockType,
                    Name = name,
                    SteamId64 = steamId64,
                    ScumId = scumId,
                    AttemptDate = date,
                    Attempts = attempts,
                    Success = succes,
                    ScumServer = _scumServer
                };
            }

            return null;
        }
    }
}
