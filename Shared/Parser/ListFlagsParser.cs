using Shared.Models;
using System.Text.RegularExpressions;

namespace Shared.Parser
{
    public class ListFlagsParser
    {
        public static List<ScumFlag> Parse(string text)
        {
            var flags = new List<ScumFlag>();
            string pattern = @"Flag ID:\s*(\d+)\s*\|\s*Owner:\s*\[(\d+)\]\s*(.+?)\s*\((\d+)\)\s*\|\s*Location:\s*X=(-?\d+\.?\d*)\s*Y=(-?\d+\.?\d*)\s*Z=(-?\d+\.?\d*)";
            var matches = Regex.Matches(text, pattern);

            foreach (Match match in matches)
            {
                var flag = new ScumFlag
                {
                    FlagId = int.Parse(match.Groups[1].Value),
                    SteamId = match.Groups[2].Value,
                    PlayerName = match.Groups[3].Value,
                    PlayerId = int.Parse(match.Groups[4].Value),
                    X = double.Parse(match.Groups[5].Value),
                    Y = double.Parse(match.Groups[6].Value),
                    Z = double.Parse(match.Groups[7].Value)
                };

                flags.Add(flag);
            }

            return flags;
        }
    }
}
