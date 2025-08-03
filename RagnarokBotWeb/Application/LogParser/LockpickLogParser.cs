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
            var pattern = @"User:\s(?<user>.+?)\s\((?<scumId>\d+),\s(?<steamId>\d+)\)\.\sSuccess:\s(?<success>Yes|No)\.\sElapsed time:\s(?<elapsed>[\d.]+)\.\sFailed attempts:\s(?<failed>\d+)\.\sTarget object:\s(?<target>.+?)\(ID:\s(?<targetId>.+?)\)\.\sLock type:\s(?<lockType>\w+)\.\sUser owner:\s(?<ownerScumId>\d+)\(\[(?<ownerSteamId>\d+)\]\s(?<ownerName>.+?)\)\.\sLocation:\sX=(?<x>[-\d.]+)\sY=(?<y>[-\d.]+)\sZ=(?<z>[-\d.]+)";

            var match = Regex.Match(logLine, pattern);

            if (!match.Success) return null;

            return new LockpickLog
            {
                User = match.Groups["user"].Value,
                ScumId = int.Parse(match.Groups["scumId"].Value),
                SteamId = match.Groups["steamId"].Value,
                Success = match.Groups["success"].Value == "Yes",
                ElapsedTime = float.Parse(match.Groups["elapsed"].Value, CultureInfo.InvariantCulture),
                FailedAttempts = int.Parse(match.Groups["failed"].Value),
                TargetObject = match.Groups["target"].Value,
                TargetId = match.Groups["targetId"].Value,
                LockType = match.Groups["lockType"].Value,
                OwnerScumId = int.Parse(match.Groups["ownerScumId"].Value),
                OwnerSteamId = match.Groups["ownerSteamId"].Value,
                OwnerName = match.Groups["ownerName"].Value,
                X = float.Parse(match.Groups["x"].Value, CultureInfo.InvariantCulture),
                Y = float.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture),
                Z = float.Parse(match.Groups["z"].Value, CultureInfo.InvariantCulture)
            };
        }
    }
}
