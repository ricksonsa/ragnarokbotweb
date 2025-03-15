using Newtonsoft.Json;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RagnarokBotWeb.Application.LogParser
{
    public class KillLogParser
    {
        private readonly List<Player> _players;
        public KillLogParser(List<Player> players)
        {
            _players = players;
        }

        public Kill Parse(string line1, string line2)
        {
            string pattern = @"Distance: ([0-9]*\.?[0-9]+) m";
            var match = Regex.Match(line1, pattern);

            if (!float.TryParse(match.Groups[1].Value, out float distance))
            {
                distance = 0;
            }

            var dateString = line1.Substring(0, line1.IndexOf(":"));
            string format = "yyyy.MM.dd-HH.mm.ss";

            var date = DateTime.ParseExact(dateString, format, CultureInfo.InvariantCulture);

            var json = line2.Substring(line2.IndexOf(":") + 2);
            var preParseKill = JsonConvert.DeserializeObject<PreParseKill>(json)!;

            return new Kill
            {
                CreateDate = date,
                Distance = distance,
                KillerSteamId64 = preParseKill.Killer.UserId,
                TargetSteamId64 = preParseKill.Victim.UserId,
                KillerName = preParseKill.Killer.ProfileName,
                TargetName = preParseKill.Victim.ProfileName,
                Weapon = preParseKill.Weapon
            };
        }
    }
}
