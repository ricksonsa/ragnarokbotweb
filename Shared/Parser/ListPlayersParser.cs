using Shared.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Shared.Parser
{
    public static class ListPlayersParser
    {
        public static List<ScumPlayer> Parse(string data)
        {
            var players = new List<ScumPlayer>();
            string[] values = data.Trim().Split(new string[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                foreach (var value in values)
                {
                    var pattern = @"^\d+\.\s*(?<name>.+)\r?\nSteam: (?<steamName>.+) \((?<steamId>\d+)\)\r?\nFame: (?<fame>\d+)\s*\r?\nAccount balance: (?<accountBalance>\d+)\r?\nGold balance: (?<goldBalance>\d+)\r?\nLocation: X=(?<x>-?\d+\.?\d*) Y=(?<y>-?\d+\.?\d*) Z=(?<z>-?\d+\.?\d*)";

                    var match = Regex.Match(value.TrimStart(), pattern, RegexOptions.Multiline);
                    if (match.Success)
                    {
                        players.Add(new ScumPlayer
                        {
                            Name = match.Groups["name"].Value,
                            SteamName = match.Groups["steamName"].Value,
                            SteamID = match.Groups["steamId"].Value,
                            Fame = int.Parse(match.Groups["fame"].Value),
                            AccountBalance = int.Parse(match.Groups["accountBalance"].Value),
                            GoldBalance = int.Parse(match.Groups["goldBalance"].Value),
                            X = double.Parse(match.Groups["x"].Value, CultureInfo.InvariantCulture),
                            Y = double.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture),
                            Z = double.Parse(match.Groups["z"].Value, CultureInfo.InvariantCulture)
                        });
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }

            return players;
        }
    }
}
