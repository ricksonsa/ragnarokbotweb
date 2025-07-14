using RagnarokBotWeb.Application.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RagnarokBotWeb.Application.LogParser
{
    public class ChatTextParser
    {
        public ChatTextParseResult? Parse(string line)
        {
            // 2025.07.13-21.42.02: '76561198002224431:Korosu 殺(1)' 'Admin: teste'

            string pattern = @"(?<date>\d{4}\.\d{2}\.\d{2}-\d{2}\.\d{2}\.\d{2}): '?(?<steamId>\d+):(?<name>.+?)'? '(?<type>Admin|Global|Local): (?<text>.+)'";

            Match match = Regex.Match(line, pattern);
            if (match.Success)
            {
                string date = match.Groups["date"].Value;
                string steamId = match.Groups["steamId"].Value;
                string playerName = match.Groups["name"].Value;
                string chatType = match.Groups["type"].Value;
                string chatText = match.Groups["text"].Value;

                return new ChatTextParseResult
                {
                    ChatType = chatType,
                    SteamId = steamId,
                    PlayerName = playerName,
                    Text = chatText,

                    Timestamp = DateTime.ParseExact(date, "yyyy.MM.dd-HH.mm.ss", CultureInfo.InvariantCulture)
                };

            }

            return null;
        }

    }
}

