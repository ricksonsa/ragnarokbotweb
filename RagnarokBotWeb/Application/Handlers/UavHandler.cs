using Discord;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;
using Shared.Models;

namespace RagnarokBotWeb.Application.Handlers
{
    public class UavHandler(
        IDiscordService discordService,
        IFileService fileService,
        List<ScumPlayer> players,
        Domain.Entities.ScumServer server
        )
    {
        public async Task Execute(Player player, string sector)
        {
            var points = players.Select(player => new ScumCoordinate(player.X, player.Y)).ToList();
            var extractor = new ScumMapExtractor(Path.Combine("cdn-storage", "scum_images", "island_4k.jpg"));
            var result = await extractor.ExtractCompleteSector(sector, points);
            var image = await fileService.SaveImageStreamAsync(result, "image/jpg", storagePath: "cdn-storage/eliminations", cdnUrlPrefix: "images/eliminations");
            var embed = new CreateEmbed()
            {
                Color = Color.DarkOrange,
                GuildId = server.Guild!.DiscordId,
                DiscordId = ulong.Parse(server.Uav.DiscordChannelId!),
                Title = $"SECTOR {sector} UAV SCAN",
                ImageUrl = image,
                TimeStamp = true
            };

            if (server.Uav.SendToUserDM)
            {
                embed.DiscordId = player.DiscordId!.Value;
                await discordService.SendEmbedToUserDM(embed);
            }
            else
            {
                embed.Text = $"Scan will expire <t:{((DateTimeOffset)DateTime.UtcNow.AddMinutes(+15)).ToUnixTimeSeconds()}:R>";
                await discordService.SendEmbedToChannel(embed);
            }

        }
    }
}
