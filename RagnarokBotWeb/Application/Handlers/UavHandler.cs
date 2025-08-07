using Discord;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Application.Handlers
{
    public class UavHandler(
        IDiscordService discordService,
        IFileService fileService,
        ICacheService cache,
        ScumServer server
        )
    {
        public async Task Handle(Player player, string sector)
        {
            var points = cache.GetConnectedPlayers(server.Id).Select(player => new ScumCoordinate(player.X, player.Y)).ToList();
            var extractor = new ScumMapExtractor(Path.Combine("cdn-storage", "scum_images", "island_4k.jpg"));
            var result = await extractor.ExtractCompleteSector(sector, points);
            var image = await fileService.SaveImageStreamAsync(result, "image/jpg", storagePath: "cdn-storage/eliminations", cdnUrlPrefix: "images/eliminations");
            var embed = new CreateEmbed()
            {
                Color = Color.DarkPurple,
                GuildId = server.Guild!.DiscordId,
                DiscordId = server.Uav.DiscordId!.Value,
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
