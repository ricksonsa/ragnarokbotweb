using Shared.Models;
using System.Text.RegularExpressions;

namespace Shared.Parser
{
    public static class ListSquadsParser
    {
        public static List<Squad> Parse(string input)
        {
            var squads = new List<Squad>();
            string[] values = Regex.Split(input.TrimStart().TrimEnd(), $"\r\n\r\n");

            foreach (string value in values)
            {
                // Match squad header
                var squadHeaderMatch = Regex.Match(value, @"\[SquadId:\s*(\d+)\s+SquadName:\s*(.+?)\]");
                if (squadHeaderMatch.Success)
                {
                    var squad = new Squad
                    {
                        SquadId = int.Parse(squadHeaderMatch.Groups[1].Value),
                        SquadName = squadHeaderMatch.Groups[2].Value
                    };

                    // Match members
                    var memberMatches = Regex.Matches(value, @"SteamId:\s*(\d+)\s+SteamName:\s*(.+?)\s+CharacterName:\s*(.+?)\s+MemberRank:\s*(\d+)");

                    foreach (Match match in memberMatches)
                    {
                        squad.Members.Add(new SquadMember
                        {
                            SteamId = match.Groups[1].Value,
                            SteamName = match.Groups[2].Value,
                            CharacterName = match.Groups[3].Value,
                            MemberRank = int.Parse(match.Groups[4].Value)
                        });
                    }

                    squads.Add(squad);
                }
            }

            return squads;
        }
    }
}
