using Shared.Models;
using System.Text.RegularExpressions;

namespace Shared.Parser
{
    public static class ListPlayersParser
    {
        public static List<ScumPlayer> ParsePlayers(string data)
        {
            var players = new List<ScumPlayer>();
            var playerSections = Regex.Split(data, @"(?=\d+\.)").Skip(1); // Split data by player sections
            playerSections = Regex.Split(data, @"\r?\n\r?\n").Where(s => s.Trim().Length > 0); // Split data by double newline

            try
            {
                foreach (var section in playerSections)
                {
                    var player = new ScumPlayer();
                    player.Name = Regex.Match(section, @"\d+\. ([^\n]+)").Groups[1].Value;

                    var steamMatch = Regex.Match(section, @"Steam: ([^\(]+) \((\d+)\)");
                    player.SteamName = steamMatch.Groups[1].Value.Trim();
                    player.SteamID = steamMatch.Groups[2].Value;

                    player.Fame = int.Parse(Regex.Match(section, @"Fame: (\d+)").Groups[1].Value);
                    player.AccountBalance = long.Parse(Regex.Match(section, @"Account balance: (\d+)").Groups[1].Value);
                    player.GoldBalance = long.Parse(Regex.Match(section, @"Gold balance: (\d+)").Groups[1].Value);
                    var locationMatch = Regex.Match(section, @"Location: X=([-\d.]+) Y=([-\d.]+) Z=([-\d.]+)");
                    player.X = float.Parse(locationMatch.Groups[1].Value);
                    player.Y = float.Parse(locationMatch.Groups[2].Value);
                    player.Z = float.Parse(locationMatch.Groups[3].Value);
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
