using Discord;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Tasks.Jobs
{
    public class BunkerStateJob(
        ILogger<BunkerStateJob> logger,
        IScumServerRepository scumServerRepository,
        IDiscordService discordService,
        IBunkerService bunkerService,
        IChannelService channelService
        ) : AbstractJob(scumServerRepository), IJob
    {
        public async Task Execute(long serverId)
        {
            logger.LogDebug("Triggered {Job} -> Execute at: {time}", $"{GetType().Name}({serverId})", DateTimeOffset.Now);

            try
            {
                var server = await GetServerAsync(serverId);

                var channel = await channelService.FindByGuildIdAndChannelTypeAsync(server.Guild!.Id, ChannelTemplateValues.BunkerActivation);
                if (channel is null) return;

                var embed = new CreateEmbed
                {
                    DiscordId = channel.DiscordId,
                    Color = Color.DarkOrange,
                    Title = "BUNKERS STATE",
                    GuildId = server.Guild.DiscordId
                };

                var bunkers = await bunkerService.FindBunkersByServer(server.Id);
                if (bunkers.Count == 0) return;

                foreach (var bunker in bunkers)
                {
                    embed.AddField(new CreateEmbedField("Sector", bunker.Sector, true));
                    embed.AddField(new CreateEmbedField("Status", bunker.Locked ? "Locked" : "Open", true));
                    string activation = $"Open Until: <t:{((DateTimeOffset)bunker.Available!.Value).ToUnixTimeSeconds()}:R>";
                    if (bunker.Locked) activation = $"Activation: <t:{((DateTimeOffset)bunker.Available!.Value).ToUnixTimeSeconds()}:R>";
                    embed.AddField(new CreateEmbedField("Time", activation, true));
                }

                await discordService.DeleteAllMessagesInChannel(embed.DiscordId);
                await discordService.SendEmbedToChannel(embed);
            }
            catch (ServerUncompliantException) { }
            catch (TenantDisabledException) { }
            catch (FtpNotSetException) { }
            catch (Exception)
            {
                throw;
            }

        }
    }
}
