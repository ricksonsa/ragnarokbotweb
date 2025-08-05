using RagnarokBotWeb.Application.Models;
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

        public static LockpickLog? Parse(string logLine)
        {
            string pattern = @"^(?<timestamp>\d{4}\.\d{2}\.\d{2}-\d{2}\.\d{2}\.\d{2}): \[LogMinigame\] \[LockpickingMinigame_C\] User: (?<username>.*?) \((?<id>\d+), (?<steamid>\d+)\)\. Success: (?<success>Yes|No)\. Elapsed time: (?<elapsedTime>\d+\.\d+)\. Failed attempts: (?<failedAttempts>\d+)\. Target object: (?<targetObject>.*?)\(ID: (?<targetId>.*?)\)\. Lock type: (?<lockType>.*?)\. User owner: (?<ownerId>\d+)\(\[(?<ownerSteamId>\d+)\] (?<ownerName>.*?)\)\. Location: X=(?<x>[\d\.-]+) Y=(?<y>[\d\.-]+) Z=(?<z>[\d\.-]+)$";
            string pattern2 = @"^(?<timestamp>\d{4}\.\d{2}\.\d{2}-\d{2}\.\d{2}\.\d{2}): \[LogMinigame\] \[LockpickingMinigame_C\] User: (?<username>.*?) \((?<id>\d+), (?<steamid>\d+)\)\. Success: (?<success>Yes|No)\. Elapsed time: (?<elapsedTime>\d+\.\d+)\. Failed attempts: (?<failedAttempts>\d+)\. Target object: (?<targetObject>.*?)\(ID: (?<targetId>.*?)\)\. Lock type: (?<lockType>.*?)\. User owner: (?<userOwner>.*?)\. Location: X=(?<x>[\d\.-]+) Y=(?<y>[\d\.-]+) Z=(?<z>[\d\.-]+)$";

            Match match = Regex.Match(logLine, pattern);
            Match match2 = Regex.Match(logLine, pattern2);
            string format = "yyyy.MM.dd-HH.mm.ss";

            if (match.Success)
            {
                try
                {
                    var lockpick = new LockpickLog
                    {
                        User = match.Groups["username"].Value,
                        ScumId = int.Parse(match.Groups["id"].Value),
                        SteamId = match.Groups["steamid"].Value,
                        Success = match.Groups["success"].Value == "Yes",
                        ElapsedTime = float.Parse(match.Groups["elapsedTime"].Value, CultureInfo.InvariantCulture),
                        FailedAttempts = int.Parse(match.Groups["failedAttempts"].Value),
                        TargetObject = match.Groups["targetObject"].Value,
                        TargetId = match.Groups["targetId"].Value,
                        LockType = match.Groups["lockType"].Value,
                        X = float.Parse(match.Groups["x"].Value, CultureInfo.InvariantCulture),
                        Y = float.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture),
                        Z = float.Parse(match.Groups["z"].Value, CultureInfo.InvariantCulture),
                        OwnerScumId = int.Parse(match.Groups["ownerScumId"].Value),
                        OwnerSteamId = match.Groups["ownerSteamId"].Value,
                        OwnerName = match.Groups["ownerName"].Value,
                        Line = logLine,
                        Date = DateTime.ParseExact(match.Groups["timestamp"].Value, format, CultureInfo.InvariantCulture)
                    };
                    return lockpick;

                }
                catch (Exception)
                {
                    return null;
                }
            }
            else if (match2.Success)
            {
                try
                {
                    var lockpick = new LockpickLog
                    {
                        User = match2.Groups["username"].Value,
                        ScumId = int.Parse(match2.Groups["id"].Value),
                        SteamId = match2.Groups["steamid"].Value,
                        Success = match2.Groups["success"].Value == "Yes",
                        ElapsedTime = float.Parse(match2.Groups["elapsedTime"].Value, CultureInfo.InvariantCulture),
                        FailedAttempts = int.Parse(match2.Groups["failedAttempts"].Value),
                        TargetObject = match2.Groups["targetObject"].Value,
                        TargetId = match2.Groups["targetId"].Value,
                        LockType = match2.Groups["lockType"].Value,
                        X = float.Parse(match2.Groups["x"].Value, CultureInfo.InvariantCulture),
                        Y = float.Parse(match2.Groups["y"].Value, CultureInfo.InvariantCulture),
                        Z = float.Parse(match2.Groups["z"].Value, CultureInfo.InvariantCulture),
                        Line = logLine,
                        Date = DateTime.ParseExact(match2.Groups["timestamp"].Value, format, CultureInfo.InvariantCulture)
                    };
                    return lockpick;
                }
                catch (Exception)
                {
                    return null;
                }

            }

            return null;
        }
    }
}
