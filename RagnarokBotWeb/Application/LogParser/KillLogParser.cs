using Newtonsoft.Json;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RagnarokBotWeb.Application.LogParser
{
    public class KillLogParser
    {
        private readonly ScumServer _server;

        public KillLogParser(ScumServer server)
        {
            _server = server;
        }

        public PreParseKill? KillParse(string line1, string line2)
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

            if (preParseKill.Killer.IsInGameEvent || preParseKill.Victim.IsInGameEvent) return null;

            preParseKill.Distance = distance;
            preParseKill.Date = TimeZoneInfo.ConvertTimeFromUtc(date, _server.GetTimeZoneOrDefault());
            preParseKill.Line = line2;
            return preParseKill;
        }

        public Kill? Parse(string line1, string line2)
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

            if (preParseKill.Killer.IsInGameEvent || preParseKill.Victim.IsInGameEvent) return null;

            var kill = new Kill
            {
                CreateDate = date,
                Distance = distance,
                KillerSteamId64 = preParseKill.Killer.UserId,
                TargetSteamId64 = preParseKill.Victim.UserId,
                KillerName = preParseKill.Killer.ProfileName,
                TargetName = preParseKill.Victim.ProfileName,
                Weapon = preParseKill.Weapon,
                ScumServer = _server,
                Sector = new ScumCoordinate(preParseKill.Victim.ClientLocation.X, preParseKill.Victim.ClientLocation.Y).GetSectorReference(),

                KillerX = preParseKill.Killer.ClientLocation.X,
                KillerY = preParseKill.Killer.ClientLocation.Y,
                KillerZ = preParseKill.Killer.ClientLocation.Z,

                VictimX = preParseKill.Victim.ClientLocation.X,
                VictimY = preParseKill.Victim.ClientLocation.Y,
                VictimZ = preParseKill.Victim.ClientLocation.Z
            };

            kill.SetHash();
            return kill;
        }

    }
}
