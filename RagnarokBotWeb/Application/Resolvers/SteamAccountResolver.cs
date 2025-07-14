using System.Text.Json;

namespace RagnarokBotWeb.Application.Resolvers
{
    public class SteamAccountResolver
    {
        public async Task<string> Resolve(string steamId)
        {
            string steamApiKey = "555A163F3AFF05F9EA2135A83DD49146"; // Replace with your actual key

            string url = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={steamApiKey}&steamids={steamId}";

            using HttpClient client = new();
            var response = await client.GetStringAsync(url);

            using JsonDocument json = JsonDocument.Parse(response);
            var root = json.RootElement;

            var players = root.GetProperty("response").GetProperty("players");
            if (players.GetArrayLength() > 0)
            {
                var player = players[0];
                string personaName = player.GetProperty("personaname").GetString();
                return personaName;
            }

            return null;
        }
    }
}
