using Shared.Models;
using System.Text.RegularExpressions;

namespace Shared.Parser
{
    public static class ListPlayersParser
    {
        public static List<ScumPlayer> ParsePlayers(string data)
        {
            var players = new List<ScumPlayer>();
            string[] lines = Regex.Split(data.TrimStart().TrimEnd(), $"\r\n\r\n");

            try
            {
                foreach (var line in lines)
                {
                    var pattern = @"^\d+\.\s*(?<name>.+)\r?\nSteam: (?<steamName>.+) \((?<steamId>\d+)\)\r?\nFame: (?<fame>\d+)\s*\r?\nAccount balance: (?<accountBalance>\d+)\r?\nGold balance: (?<goldBalance>\d+)\r?\nLocation: X=(?<x>-?\d+\.?\d*) Y=(?<y>-?\d+\.?\d*) Z=(?<z>-?\d+\.?\d*)";

                    var match = Regex.Match(line, pattern, RegexOptions.Multiline);
                    if (match.Success)
                    {
                        var player = new ScumPlayer();
                        player.Name = match.Groups["name"].Value;
                        player.SteamName = match.Groups["steamName"].Value;
                        player.SteamID = match.Groups["steamId"].Value;
                        player.Fame = int.Parse(match.Groups["fame"].Value);
                        player.AccountBalance = int.Parse(match.Groups["accountBalance"].Value);
                        player.GoldBalance = int.Parse(match.Groups["goldBalance"].Value);
                        player.X = float.Parse(match.Groups["x"].Value);
                        player.Y = float.Parse(match.Groups["y"].Value);
                        player.Z = float.Parse(match.Groups["z"].Value);
                        players.Add(player);
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
