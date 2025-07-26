using Shared.Models;
using System.Text.RegularExpressions;

namespace Shared.Parser
{
    public static class ListPlayersParser
    {
        public static List<ScumPlayer> ParsePlayers(string data)
        {
            var players = new List<ScumPlayer>();
            // Line 1: "1. Korosu 殺"
            var nameRegex = new Regex(@"^(?<id>\d+)\.\s+(?<name>.+)$");

            // Line 2: "Steam: Korosu 殺 (76561198002224431)"
            var steamRegex = new Regex(@"^Steam:\s+(?<steamName>.+)\s+\((?<steamId>\d+)\)$");

            // Line 3: "Fame: 23365"
            var fameRegex = new Regex(@"^Fame:\s+(?<fame>\d+)$");

            // Line 4: "Account balance: 13132475"
            var accountRegex = new Regex(@"^Account balance:\s+(?<account>-?\d+)$");

            // Line 5: "Gold balance: 50"
            var goldRegex = new Regex(@"^Gold balance:\s+(?<gold>-?\d+)$");

            // Line 6: "Location: X=500248.906 Y=-547572.750 Z=3996.380"
            var locationRegex = new Regex(@"^Location:\s+X=(?<x>-?[\d.]+)\s+Y=(?<y>-?[\d.]+)\s+Z=(?<z>-?[\d.]+)$");

            string[] lines = Regex.Split(data.TrimStart().TrimEnd(), $"\r\n\r\n");
            try
            {
                foreach (var line in lines)
                {
                    var player = new ScumPlayer();
                    if (nameRegex.Match(line) is Match m1 && m1.Success)
                    {
                        player.Name = m1.Groups["name"].Value.Trim();
                    }
                    else if (steamRegex.Match(line) is Match m2 && m2.Success)
                    {
                        player.SteamName = m2.Groups["steamName"].Value.Trim();
                        player.SteamID = m2.Groups["steamId"].Value;
                    }
                    else if (fameRegex.Match(line) is Match m3 && m3.Success)
                    {
                        player.Fame = int.Parse(m3.Groups["fame"].Value);
                    }
                    else if (accountRegex.Match(line) is Match m4 && m4.Success)
                    {
                        player.AccountBalance = int.Parse(m4.Groups["account"].Value);
                    }
                    else if (goldRegex.Match(line) is Match m5 && m5.Success)
                    {
                        player.GoldBalance = int.Parse(m5.Groups["gold"].Value);
                    }
                    else if (locationRegex.Match(line) is Match m6 && m6.Success)
                    {
                        player.X = float.Parse(m6.Groups["x"].Value);
                        player.Y = float.Parse(m6.Groups["y"].Value);
                        player.Z = float.Parse(m6.Groups["z"].Value);
                    }
                    players.Add(player);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }


            return players;
        }
    }
}
