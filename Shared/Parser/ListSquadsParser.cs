using Shared.Models;
using System.Text.RegularExpressions;

namespace Shared.Parser
{
    public static class ListSquadsParser
    {
        private static readonly Regex SquadRegex =
       new(@"\[SquadId:\s*(\d+)\s*SquadName:\s*(.+?)\]", RegexOptions.Compiled);

        private static readonly Regex MemberRegex =
            new(@"SteamId:\s*(\d+)\s*SteamName:\s*(.+?)\s*CharacterName:\s*(.+?)\s*MemberRank:\s*(\d+)", RegexOptions.Compiled);

        public static List<ScumSquad> Parse(string input)
        {
            var squads = new List<ScumSquad>();
            ScumSquad? currentSquad = null;

            foreach (var rawLine in input.Split('\n'))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Check if this line defines a squad
                var squadMatch = SquadRegex.Match(line);
                if (squadMatch.Success)
                {
                    currentSquad = new ScumSquad
                    {
                        SquadId = int.Parse(squadMatch.Groups[1].Value),
                        SquadName = squadMatch.Groups[2].Value.Trim()
                    };
                    squads.Add(currentSquad);
                    continue;
                }

                // Check if this line defines a member
                var memberMatch = MemberRegex.Match(line);
                if (memberMatch.Success && currentSquad != null)
                {
                    var member = new SquadMember
                    {
                        SteamId = memberMatch.Groups[1].Value,
                        SteamName = memberMatch.Groups[2].Value.Trim(),
                        CharacterName = memberMatch.Groups[3].Value.Trim(),
                        MemberRank = int.Parse(memberMatch.Groups[4].Value)
                    };
                    currentSquad.Members.Add(member);
                }
            }

            return squads;
        }
    }
}
