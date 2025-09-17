using System.Text.Json;

namespace RagnarokBotWeb.Application.Resolvers
{
    public class SteamAccountResolver
    {
        private readonly HttpClient _httpClient;
        private readonly string _steamApiKey = "555A163F3AFF05F9EA2135A83DD49146"; // TODO: move to config/secret store

        public SteamAccountResolver(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<SteamAccountResult?> Resolve(string steamId)
        {
            string profileUrl =
                $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={_steamApiKey}&steamids={steamId}";

            string bansUrl =
                $"https://api.steampowered.com/ISteamUser/GetPlayerBans/v1/?key={_steamApiKey}&steamids={steamId}";

            // Fire both requests in parallel
            var profileTask = _httpClient.GetStringAsync(profileUrl);
            var bansTask = _httpClient.GetStringAsync(bansUrl);

            await Task.WhenAll(profileTask, bansTask);

            // Parse profile
            using var profileJson = JsonDocument.Parse(profileTask.Result);
            var players = profileJson.RootElement.GetProperty("response").GetProperty("players");

            if (players.GetArrayLength() == 0)
                return null;

            var player = players[0];
            string? personaName = player.GetProperty("personaname").GetString();

            // Parse bans
            using var bansJson = JsonDocument.Parse(bansTask.Result);
            var banInfo = bansJson.RootElement.GetProperty("players")[0];

            return new SteamAccountResult
            {
                SteamId = steamId,
                PersonaName = personaName,
                VacBanned = banInfo.GetProperty("VACBanned").GetBoolean(),
                NumberOfVacBans = banInfo.GetProperty("NumberOfVACBans").GetInt32(),
                DaysSinceLastBan = banInfo.GetProperty("DaysSinceLastBan").GetInt32(),
                NumberOfGameBans = banInfo.GetProperty("NumberOfGameBans").GetInt32(),
                CommunityBanned = banInfo.GetProperty("CommunityBanned").GetBoolean()
            };
        }
    }

    public class SteamAccountResult
    {
        public string SteamId { get; set; } = string.Empty;
        public string? PersonaName { get; set; }
        public bool VacBanned { get; set; }
        public int NumberOfVacBans { get; set; }
        public int DaysSinceLastBan { get; set; }
        public int NumberOfGameBans { get; set; }
        public bool CommunityBanned { get; set; }
    }
}
