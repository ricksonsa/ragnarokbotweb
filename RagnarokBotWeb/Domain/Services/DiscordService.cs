using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using RagnarokBotWeb.Application.Discord;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Configuration.Data;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;
using System.Text;
using static RagnarokBotWeb.Application.Tasks.Jobs.KillRankJob;
using static RagnarokBotWeb.Application.Tasks.Jobs.LockpickRankJob;
using Color = Discord.Color;

namespace RagnarokBotWeb.Domain.Services
{
    public class DiscordService : IDiscordService
    {
        private readonly ILogger<DiscordService> _logger;
        private readonly DiscordSocketClient _client;
        private readonly AppSettings _appSettings;
        private readonly IGuildService _guildService;
        private readonly StartupDiscordTemplate _startupDiscordTemplate;
        private readonly IChannelService _channelService;

        public DiscordService(
            ILogger<DiscordService> logger,
            DiscordSocketClient client,
            IGuildService guildService,
            StartupDiscordTemplate startupDiscordTemplate,
            IChannelService channelService,
            IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _client = client;
            _guildService = guildService;
            _startupDiscordTemplate = startupDiscordTemplate;
            _channelService = channelService;
            _appSettings = appSettings.Value;
        }

        public async Task<Guild> CreateChannelTemplates(long serverId)
        {
            var guild = await _guildService.FindByServerIdAsync(serverId);
            if (guild is null)
            {
                _logger.LogWarning("No guild found.");
                throw new DomainException("No guild found");
            }
            //await ClearBefore(guild);

            await _startupDiscordTemplate.Run(guild);
            guild.RunTemplate = true;
            await _guildService.Update(guild);
            return (await _guildService.FindByServerIdAsync(serverId))!;
        }

        private EmbedAuthorBuilder GetAuthor()
        {
            return new EmbedAuthorBuilder().WithName("RAGNAROK BOT").WithIconUrl(_appSettings.BaseUrl + "/images/ragnarok-logo.png");
        }

        private async Task ClearBefore(Guild guild)
        {
            guild.RunTemplate = false;

            await _guildService.Update(guild);
            await _channelService.DeleteAllAsync();

            var socketGuild = _client.GetGuild(guild.DiscordId);
            foreach (var channel in socketGuild.Channels) await channel.DeleteAsync();
            foreach (var category in socketGuild.CategoryChannels) await category.DeleteAsync();
        }

        public string GetDiscordUserName(ulong discordId)
        {
            var user = _client.GetUser(discordId);
            if (user is null)
            {
                return null;
            }

            return user.Username;
        }

        public async Task SendEmbedToUserDM(CreateEmbed createEmbed)
        {
            var user = await GetDiscordUser(createEmbed.GuildId, createEmbed.DiscordId);
            if (user is null) return;

            var dmChannel = await user.CreateDMChannelAsync();

            var embedBuilder = new EmbedBuilder()
                .WithTitle(createEmbed.Title)
                .WithDescription(createEmbed.Text)
                .WithAuthor(GetAuthor())
                .WithCurrentTimestamp()
                .WithColor(Color.DarkPurple);

            if (!string.IsNullOrEmpty(createEmbed.ImageUrl)) embedBuilder.WithImageUrl(createEmbed.ImageUrl);

            var builder = new ComponentBuilder();

            createEmbed.Buttons.ForEach(button =>
            {
                builder.WithButton(
                    label: button.Label,
                    customId: button.ActionId,
                    style: ButtonStyle.Primary);
            });

            await dmChannel.SendMessageAsync(embed: embedBuilder.Build(), components: builder.Build());
        }

        public async Task<IUserMessage> SendEmbedWithBase64Image(CreateEmbed createEmbed)
        {

            var channel = _client.GetChannel(createEmbed.DiscordId) as IMessageChannel;

            if (channel != null)
            {
                var base64Data = createEmbed.ImageUrl.Split(',').Last();
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                var stream = new MemoryStream(imageBytes);
                var filename = $"pack-{createEmbed.Title.Trim()}.png";

                var embed = new EmbedBuilder()
                    .WithTitle(createEmbed.Title)
                    .WithAuthor(GetAuthor())
                    .WithDescription(createEmbed.Text)
                    .WithImageUrl("attachment://" + filename)
                    .WithFooter(new EmbedFooterBuilder { Text = createEmbed.FooterText })
                    .WithColor(createEmbed.Color)
                    .Build();

                var builder = new ComponentBuilder();

                createEmbed.Buttons.ForEach(button =>
                {
                    builder.WithButton(
                        label: button.Label,
                        customId: button.ActionId,
                        style: ButtonStyle.Primary);
                });

                return await channel.SendFileAsync(stream, filename, embed: embed, components: builder.Build());
            }

            return null;
        }

        public async Task DeleteAllMessagesInChannel(ulong channelId)
        {
            var channel = _client.GetChannel(channelId) as IMessageChannel;
            if (channel == null)
            {
                Console.WriteLine("Channel not found or is not a message channel.");
                return;
            }

            int deletedCount = 0;
            bool hasMore = true;

            while (hasMore)
            {
                var messages = await channel.GetMessagesAsync(100).FlattenAsync();
                var messageList = messages.ToList();

                if (messageList.Count == 0)
                {
                    hasMore = false;
                    break;
                }

                foreach (var msg in messageList)
                {
                    try
                    {
                        await msg.DeleteAsync();
                        deletedCount++;
                        await Task.Delay(500); // Avoid rate limit
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete message: {ex.Message}");
                    }
                }
            }

            Console.WriteLine($"Deleted {deletedCount} messages.");
        }

        public async Task SendLockpickRankEmbed(
            ulong channelId,
           List<LockpickStatsDto> stats,
           string lockType)
        {
            var channel = _client.GetChannel(channelId) as IMessageChannel;
            if (channel == null) return;

            var builder = new EmbedBuilder()
                .WithTitle($"🔐 TOP LOCK PICKERS - {lockType}")
                .WithColor(Color.DarkPurple)
                .WithCurrentTimestamp();

            var sb = new StringBuilder();
            sb.AppendLine("```");
            sb.AppendLine("RANK | Player               | Success | Fails |   %");
            sb.AppendLine("-----|----------------------|---------|-------|--------");

            int rank = 1;
            foreach (var p in stats)
            {
                sb.AppendLine($"{rank,4} | {Truncate(p.PlayerName, 20),-20} | {p.SuccessCount,7} | {p.FailCount,5} | {p.SuccessRate,6:F2}%");
                rank++;
            }

            sb.AppendLine("```");

            builder.WithDescription(sb.ToString());

            await channel.SendMessageAsync(embed: builder.Build());
        }

        public async Task SendTopDistanceKillsEmbed(
            ulong channelId,
            List<PlayerStatsDto> players,
            int topCount)
        {
            var channel = _client.GetChannel(channelId) as IMessageChannel;
            if (channel == null) return;

            var builder = new EmbedBuilder()
             .WithTitle($"{DiscordEmoji.Dart} SNIPER RANK")
             .WithColor(Color.DarkPurple)
             .WithCurrentTimestamp();

            var sb = new StringBuilder();
            sb.AppendLine("```");
            sb.AppendLine("#  | Player             | Distance (m) | Weapon");
            sb.AppendLine("---|--------------------|--------------|----------------");

            int rank = 1;
            foreach (var p in players)
            {
                sb.AppendLine($"{rank,2} | {Truncate(p.PlayerName, 18),-18} | {p.KillDistance,12:F1} | {Truncate(p.WeaponName, 16)}");
                rank++;
            }

            sb.AppendLine("```");

            builder.WithDescription(sb.ToString());

            await channel.SendMessageAsync(embed: builder.Build());
        }


        public async Task SendTopPlayersKillsEmbed(ulong channelId, List<PlayerStatsDto> players, ERankingPeriod period, int topCount)
        {
            var channel = _client.GetChannel(channelId) as IMessageChannel;
            if (channel == null) return;

            var builder = new EmbedBuilder()
            .WithTitle($"🏆 Top {topCount} Killers ({period})")
            .WithColor(Color.Purple)
            .WithCurrentTimestamp();

            var sb = new StringBuilder();
            sb.AppendLine("```");
            sb.AppendLine("#  | Player             | Kills | Deaths");
            sb.AppendLine("---|--------------------|-------|--------");

            int rank = 1;
            foreach (var p in players)
            {
                sb.AppendLine($"{rank,2} | {Truncate(p.PlayerName, 18),-18} | {p.KillCount,5} | {p.DeathCount,6}");
                rank++;
            }

            sb.AppendLine("```");

            builder.WithDescription(sb.ToString());

            await channel.SendMessageAsync(embed: builder.Build());
        }

        private string Truncate(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 1) + "…";
        }


        public async Task<IUserMessage> SendEmbedToChannel(CreateEmbed createEmbed)
        {
            var channel = _client.GetChannel(createEmbed.DiscordId) as IMessageChannel;

            if (channel != null)
            {
                var embedBuilder = new EmbedBuilder()
                    .WithTitle(createEmbed.Title)
                    .WithDescription(createEmbed.Text)
                    .WithAuthor(GetAuthor())
                    .WithImageUrl(_appSettings.BaseUrl + "/" + createEmbed.ImageUrl)
                    .WithFooter(new EmbedFooterBuilder { Text = createEmbed.FooterText })
                    .WithColor(createEmbed.Color);

                var builder = new ComponentBuilder();

                createEmbed.Buttons.ForEach(button =>
                {
                    builder.WithButton(
                        label: button.Label,
                        customId: button.ActionId,
                        style: ButtonStyle.Primary);
                });


                createEmbed.Fields.ForEach(field =>
                {
                    embedBuilder.AddField(
                        new EmbedFieldBuilder()
                        .WithName(field.Title)
                        .WithValue(field.Message)
                        .WithIsInline(field.Inline));
                });

                return await channel.SendMessageAsync(embed: embedBuilder.Build(), components: builder.Build());
            }

            return null;
        }



        public async Task RemoveMessage(ulong channelId, ulong messageId)
        {
            try
            {
                var channel = await _client.GetChannelAsync(channelId) as IMessageChannel;

                if (channel != null)
                {
                    var message = await channel.GetMessageAsync(messageId);
                    if (message != null)
                    {
                        await message.DeleteAsync();
                    }
                }
            }
            catch (Exception)
            {
                _logger.LogError("Could not remove message[{Message}] from channel[{Channel}]", messageId, channelId);
            }

        }

        public async Task<IUserMessage?> CreateButtonAsync(ulong discordId, ButtonTemplate buttonTemplate)
        {
            var channel = await _client.GetChannelAsync(discordId) as IMessageChannel;

            if (channel != null)
            {
                var button = new ButtonBuilder()
                    .WithLabel(buttonTemplate.Name)
                    .WithCustomId(buttonTemplate.Command)
                    .WithStyle(ButtonStyle.Primary);

                var messageComponent = new ComponentBuilder()
                    .WithButton(button)
                    .Build();

                return await channel.SendMessageAsync(components: messageComponent);
            }

            return null;
        }

        public Task<IGuildUser?> GetDiscordUser(ulong guildId, ulong userId)
        {
            var socketGuild = _client.GetGuild(guildId); // SocketGuild
            IGuild guild = socketGuild; // Pode ser usado como IGuild

            return guild.GetUserAsync(userId);
        }

        public async Task AddUserRoleAsync(ulong guildId, ulong userDiscordId, ulong roleId)
        {
            var socketGuild = _client.GetGuild(guildId); // SocketGuild
            IGuild guild = socketGuild; // Pode ser usado como IGuild

            var user = await guild.GetUserAsync(userDiscordId);
            if (user == null)
            {
                _logger.LogError("User not found with discordId[{Id}]", userDiscordId);
                return;
            }

            var role = guild.GetRole(roleId);
            if (role == null)
            {
                _logger.LogError("Discord Role not found with Id[{Id}]", roleId);
                return;
            }

            await user.AddRoleAsync(role);
            _logger.LogInformation("Added Discord Role Id[{Role}] to user[{User}]", roleId, userDiscordId);
        }

        public async Task RemoveUserRoleAsync(ulong guildId, ulong userDiscordId, ulong roleId)
        {
            var socketGuild = _client.GetGuild(guildId); // SocketGuild
            IGuild guild = socketGuild; // Pode ser usado como IGuild

            var user = await guild.GetUserAsync(userDiscordId);
            if (user == null)
            {
                _logger.LogError("User not found with discordId[{Id}]", userDiscordId);
                return;
            }

            var role = guild.GetRole(roleId);
            if (role == null)
            {
                _logger.LogError("Discord Role not found with Id[{Id}]", roleId);
                return;
            }

            await user.RemoveRoleAsync(role);
            _logger.LogInformation("Removed Discord Role Id[{Role}] to user[{User}]", roleId, userDiscordId);
        }

        public async Task SendKillFeedEmbed(ScumServer server, Kill kill)
        {
            var killFeedChannel = await _channelService.FindByGuildIdAndChannelTypeAsync(server.Guild!.Id, ChannelTemplateValues.KillFeed);
            if (killFeedChannel is null) return;
            var channel = await _client.GetChannelAsync(killFeedChannel.DiscordId) as IMessageChannel;
            if (channel is null) return;

            var embedBuilder = new EmbedBuilder()
                .WithColor(Color.DarkPurple)
                .WithAuthor(GetAuthor())
                .WithCurrentTimestamp();

            if (server.ShowKillerName)
            {
                embedBuilder.AddField("Killer", kill.KillerName, true);
                embedBuilder.AddField("Victim", kill.TargetName, true);
            }
            else
            {
                embedBuilder.AddField("Victim", kill.TargetName, false);
            }

            if (server.ShowKillWeapon)
                embedBuilder.AddField("Weapon", kill.DisplayWeapon, false);

            if (server.ShowKillDistance)
                embedBuilder.AddField("Distance", kill.Distance, false);

            if (server.ShowKillSector)
                embedBuilder.AddField("Sector", kill.Sector, false);

            if (server.ShowKillCoordinates)
            {
                embedBuilder.AddField("Killer Coordinates", $"{kill.KillerX} {kill.KillerY} {kill.KillerZ}", true);
                embedBuilder.AddField("Victim Coordinates", $"{kill.VictimX} {kill.VictimY} {kill.VictimZ}", true);
            }

            if (server.ShowKillOnMap)
            {
                try
                {
                    embedBuilder.WithImageUrl($"{_appSettings.BaseUrl}/{kill.ImageUrl}");
                }
                catch (Exception)
                { }
            }

            try
            {
                embedBuilder.WithThumbnailUrl($"{_appSettings.BaseUrl}/images/scum_images/{kill.Weapon!.Substring(0, kill.Weapon.LastIndexOf("_C"))}.webp");
            }
            catch (Exception)
            { }

            await channel.SendMessageAsync(embed: embedBuilder.Build());
        }
    }
}
