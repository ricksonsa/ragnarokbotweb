using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using RagnarokBotWeb.Application.Discord;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Configuration.Data;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
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
        private readonly IChannelService _channelService;

        public DiscordService(
            ILogger<DiscordService> logger,
            DiscordSocketClient client,
            IGuildService guildService,
            IChannelService channelService,
            IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _client = client;
            _guildService = guildService;
            _channelService = channelService;
            _appSettings = appSettings.Value;
        }

        private EmbedFooterBuilder GetAuthor()
        {
            return new EmbedFooterBuilder().WithText("www.thescumbot.com").WithIconUrl(_appSettings.BaseUrl + "/images/bot.png");
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

        public async Task<IUserMessage> SendEmbedToChannel(CreateEmbed createEmbed)
        {
            var channel = _client.GetChannel(createEmbed.DiscordId) as IMessageChannel;

            if (channel != null)
            {
                var embedBuilder = new EmbedBuilder()
                    .WithTitle(createEmbed.Title)
                    .WithDescription(createEmbed.Text)
                    .WithFooter(GetAuthor())
                    .WithColor(createEmbed.Color);


                if (!string.IsNullOrEmpty(createEmbed.ImageUrl)) embedBuilder.WithImageUrl($"{_appSettings.BaseUrl}/{createEmbed.ImageUrl}");
                if (createEmbed.TimeStamp) embedBuilder.WithCurrentTimestamp();

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

        public async Task SendEmbedToUserDM(CreateEmbed createEmbed)
        {
            var user = await GetDiscordUser(createEmbed.GuildId, createEmbed.DiscordId);
            if (user is null) return;

            var dmChannel = await user.CreateDMChannelAsync();

            var embedBuilder = new EmbedBuilder()
                .WithTitle(createEmbed.Title)
                .WithDescription(createEmbed.Text)
                .WithFooter(GetAuthor())
                .WithColor(createEmbed.Color);

            if (!string.IsNullOrEmpty(createEmbed.ImageUrl)) embedBuilder.WithImageUrl($"{_appSettings.BaseUrl}/{createEmbed.ImageUrl}");
            if (createEmbed.TimeStamp) embedBuilder.WithCurrentTimestamp();

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
                    .WithFooter(GetAuthor())
                    .WithDescription(createEmbed.Text)
                    .WithImageUrl("attachment://" + filename)
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

        public async Task DeleteAllMessagesInChannelByDate(ulong channelId, DateTime date)
        {
            var channel = _client.GetChannel(channelId) as IMessageChannel;
            if (channel == null)
            {
                Console.WriteLine("Channel not found or is not a message channel.");
                return;
            }

            int deletedCount = 0;
            bool hasMore = true;
            IMessage? lastMessage = null;

            while (hasMore)
            {
                IEnumerable<IMessage> messages;

                if (lastMessage == null)
                    messages = await channel.GetMessagesAsync(100, CacheMode.AllowDownload).FlattenAsync();
                else
                    messages = await channel.GetMessagesAsync(lastMessage, Direction.Before, 100, CacheMode.AllowDownload).FlattenAsync();

                var messageList = messages.ToList();

                if (messageList.Count == 0)
                {
                    hasMore = false;
                    break;
                }

                foreach (var msg in messageList)
                {
                    if (msg.Timestamp.UtcDateTime < date.ToUniversalTime() && msg.Embeds.Any(e => e.Description.Contains("Scan will expire")))
                    {
                        try
                        {
                            await msg.DeleteAsync();
                            deletedCount++;
                            await Task.Delay(500); // Delay to avoid rate limit
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to delete message: {ex.Message}");
                        }
                    }
                }

                lastMessage = messageList.LastOrDefault();
                if (lastMessage == null)
                {
                    hasMore = false;
                }
            }

            Console.WriteLine($"Deleted {deletedCount} messages older than {date:u}.");
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
                .WithColor(Color.DarkOrange)
                .WithFooter(GetAuthor())
                .WithCurrentTimestamp();

            var sb = new StringBuilder();
            sb.AppendLine("```");
            sb.AppendLine($"{"#",2} | {"Player",-12} | {"Success",7} | {"Attempts",8} | {"%",5}");
            sb.AppendLine("---|--------------|---------|----------|------");

            int rank = 1;
            foreach (var p in stats)
            {
                sb.AppendLine(
                    $"{rank,2} | {Truncate(p.PlayerName, 12),-12} | {p.SuccessCount,7} | {p.Attempts,8} | {p.SuccessRate,5:F1}%"
                );
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
             .WithColor(Color.DarkOrange)
             .WithFooter(GetAuthor())
             .WithCurrentTimestamp();

            var sb = new StringBuilder();
            sb.AppendLine("```");
            sb.AppendLine("#  | Player             | Distance (m) | Weapon");
            sb.AppendLine("---|--------------------|--------------|----------------");

            int rank = 1;
            foreach (var p in players)
            {
                sb.AppendLine($"{rank,2} | {Truncate(p.PlayerName, 18),-18} | {Convert.ToInt32(Math.Round(p.KillDistance / 100f)),12:F1} | {Truncate(p.WeaponName, 16)}");
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
            .WithColor(Color.DarkOrange)
            .WithFooter(GetAuthor())
            .WithCurrentTimestamp();

            var sb = new StringBuilder();
            sb.AppendLine("```");
            sb.AppendLine("#  | Player             | Kills | Deaths");
            sb.AppendLine("---|--------------------|-------|--------");

            int rank = 1;
            foreach (var p in players)
            {
                sb.AppendLine($"{rank,2}  {Truncate(p.PlayerName, 18),-18}  {p.KillCount,5}  {p.DeathCount,6}");
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

        public Task<IUserMessage> SendMessageToChannel(string message, ulong channelId)
        {
            var channel = _client.GetChannel(channelId) as IMessageChannel;
            if (channel != null) return channel.SendMessageAsync(message);
            throw new Exception("Channel not found");
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

        public async Task<IUserMessage?> CreateTeleportButtons(Taxi taxi)
        {
            if (taxi.ScumServer.Guild is null) return null;

            var socketGuild = _client.GetGuild(taxi.ScumServer.Guild.DiscordId); // SocketGuild
            IGuild guild = socketGuild; // Pode ser usado como IGuild

            var channel = await _client.GetChannelAsync(ulong.Parse(taxi.DiscordChannelId!)) as IMessageChannel;
            if (channel is null) return null;

            var sectorOptions = taxi.TaxiTeleports.
                Select(taxiTeleport =>
                new SelectMenuOptionBuilder(taxiTeleport.Teleport.Name, taxiTeleport.Id.ToString()))
                .Prepend(new SelectMenuOptionBuilder("None", "0"))
                .ToList();

            var sectorsSelectMenu = new SelectMenuBuilder()
                .WithCustomId("taxi_telport_select")
                .WithPlaceholder("Choose a destination")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithOptions(sectorOptions);

            var button = new ButtonBuilder()
                .WithLabel($"Buy {taxi.Name}")
                .WithStyle(ButtonStyle.Primary)
                .WithCustomId("buy_taxi");

            var component = new ComponentBuilder()
                .WithSelectMenu(sectorsSelectMenu, row: 0)
                .WithButton(button, row: 1);

            var embedBuilder = new EmbedBuilder()
                .WithTitle(taxi.Name)
                .WithDescription(taxi.Description)
                .WithColor(Color.DarkOrange)
                .WithFooter(GetAuthor());

            if (taxi.Price > 0)
                embedBuilder.AddField(new EmbedFieldBuilder().WithName("Price").WithValue(taxi.Price).WithIsInline(true));

            if (taxi.VipPrice > 0)
                embedBuilder.AddField(new EmbedFieldBuilder().WithName("Vip Price").WithValue(taxi.VipPrice).WithIsInline(true));

            if (taxi.ImageUrl != null)
                embedBuilder.WithImageUrl($"{_appSettings.BaseUrl}/{taxi.ImageUrl}");

            _logger.LogDebug("Taxi Buttons created for guild[{Guild}] channel[{Channel}]", taxi.ScumServer.Guild.DiscordId, taxi.DiscordChannelId);
            return await channel.SendMessageAsync(embed: embedBuilder.Build(), components: component.Build());
        }

        public async Task<IUserMessage?> CreateUavButtons(ScumServer server, ulong channelId)
        {
            if (server.Guild is null) return null;

            var socketGuild = _client.GetGuild(server.Guild.DiscordId); // SocketGuild
            IGuild guild = socketGuild; // Pode ser usado como IGuild

            var channel = await _client.GetChannelAsync(channelId) as IMessageChannel;
            if (channel is null) return null;

            var sectors = SectorDefinitions.SECTOR_Y_CENTERS.Select(row => row.Key.ToString()).ToList();
            List<string> sectorValues = [];

            foreach (var sector in sectors)
                for (int i = 0; i < SectorDefinitions.SECTOR_X_CENTERS.Values.Count; i++)
                    sectorValues.Add($"{sector}{i}");

            var sectorOptions = sectorValues
                .Select(sector => new SelectMenuOptionBuilder(sector, sector))
                .ToList();

            var sectorsSelectMenu = new SelectMenuBuilder()
                .WithCustomId("uav_zone_select")
                .WithPlaceholder("Choose a zone")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithOptions(sectorOptions);

            var button = new ButtonBuilder()
                .WithLabel("Scan")
                .WithStyle(ButtonStyle.Primary)
                .WithCustomId("uav_scan_trigger");

            var component = new ComponentBuilder()
                .WithSelectMenu(sectorsSelectMenu, row: 0)
                .WithButton(button, row: 1);

            var embedBuilder = new EmbedBuilder()
                .WithTitle(server.Uav.Name)
                .WithDescription(server.Uav.Description)
                .WithColor(Color.DarkOrange)
                .WithFooter(GetAuthor());

            if (server.Uav.Price > 0)
                embedBuilder.AddField(new EmbedFieldBuilder().WithName("Price").WithValue(server.Uav.Price).WithIsInline(true));

            if (server.Uav.VipPrice > 0)
                embedBuilder.AddField(new EmbedFieldBuilder().WithName("Vip Price").WithValue(server.Uav.VipPrice).WithIsInline(true));

            if (server.Uav.ImageUrl != null)
                embedBuilder.WithImageUrl($"{_appSettings.BaseUrl}/{server.Uav.ImageUrl}");

            _logger.LogDebug("UAV Button created form guild[{Guild}] channel[{Channel}]", server.Guild.DiscordId, channelId);
            return await channel.SendMessageAsync(embed: embedBuilder.Build(), components: component.Build());
        }

        public async Task<IUserMessage?> CreateExchangeButtons(ScumServer server)
        {
            if (server.Guild is null) return null;
            if (server.Exchange is null) return null;
            if (!server.Exchange.Enabled) return null;

            //var socketGuild = _client.GetGuild(server.Guild.DiscordId); // SocketGuild
            //IGuild guild = socketGuild; // Pode ser usado como IGuild

            var channel = await _client.GetChannelAsync(ulong.Parse(server.Exchange.DiscordChannelId!)) as IMessageChannel;
            if (channel is null) return null;

            var embedBuilder = new EmbedBuilder()
                .WithTitle(server.Exchange.Name)
                .WithFooter(GetAuthor())
                .WithDescription(server.Exchange.Description)
                .WithColor(Color.DarkOrange);

            var componentBuilder = new ComponentBuilder();

            if (server.Exchange.AllowTransfer)
            {
                componentBuilder.WithButton(
                  label: "Transfer",
                  customId: "transfer_trigger",
                  style: ButtonStyle.Primary);
            }

            if (server.Exchange.AllowWithdraw)
            {
                componentBuilder.WithButton(
                  label: "Withdraw",
                  customId: "withdraw_trigger",
                  style: ButtonStyle.Primary);
            }

            if (server.Exchange.AllowDeposit)
            {
                componentBuilder.WithButton(
                  label: "Deposit",
                  customId: "deposit_trigger",
                  style: ButtonStyle.Primary);
            }

            if (server.Exchange.ImageUrl != null)
                embedBuilder.WithImageUrl($"{_appSettings.BaseUrl}/{server.Exchange.ImageUrl}");

            _logger.LogDebug("Exchange Buttons created for guild[{Guild}] channel[{Channel}]", server.Guild.DiscordId, server.Exchange.DiscordChannelId);
            return await channel.SendMessageAsync(embed: embedBuilder.Build(), components: componentBuilder.Build());
        }

        public async Task SendKillFeedEmbed(ScumServer server, Kill kill)
        {
            var killFeedChannel = await _channelService.FindByGuildIdAndChannelTypeAsync(server.Guild!.Id, ChannelTemplateValues.KillFeed);
            if (killFeedChannel is null) return;
            var channel = await _client.GetChannelAsync(killFeedChannel.DiscordId) as IMessageChannel;
            if (channel is null) return;

            var embedBuilder = new EmbedBuilder()
                .WithColor(Color.DarkOrange)
                .WithFooter(GetAuthor())
                .WithTimestamp(new DateTimeOffset(kill.CreateDate, TimeSpan.Zero));

            if (server.ShowKillerName)
            {
                embedBuilder.AddField(server.ShowKillOnMap ? "Killer [red]" : "Killer", kill.KillerName, true);
                embedBuilder.AddField(server.ShowKillOnMap ? "Victim [black]" : "Victim", kill.TargetName, true);
            }
            else
            {
                embedBuilder.AddField("Victim", kill.TargetName, false);
            }

            if (server.ShowKillWeapon)
                embedBuilder.AddField("Weapon", kill.DisplayWeapon, false);

            if (server.ShowKillDistance)
                embedBuilder.AddField("Distance", kill.Distance.HasValue ? $"{Convert.ToInt32(Math.Round(kill.Distance.Value / 100f))}m" : "Unknown", false);

            if (server.ShowKillSector)
                embedBuilder.AddField("Sector", kill.Sector, false);

            if (server.ShowKillCoordinates)
            {
                embedBuilder.AddField("Killer Coordinates", $"{new ScumCoordinate(kill.KillerX, kill.KillerY, kill.KillerZ)}", true);
                embedBuilder.AddField("Victim Coordinates", $"{new ScumCoordinate(kill.VictimX, kill.VictimY, kill.VictimZ)}", true);
            }

            if (server.ShowKillOnMap)
            {
                try
                {
                    embedBuilder.WithImageUrl($"{_appSettings.BaseUrl}/{kill.ImageUrl}");
                }
                catch { }
            }

            try
            {
                embedBuilder.WithThumbnailUrl($"{_appSettings.BaseUrl}/images/scum_images/{kill.Weapon!.Substring(0, kill.Weapon.LastIndexOf("_C"))}.webp");
            }
            catch { }

            try
            {
                await channel.SendMessageAsync(embed: embedBuilder.Build());

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendKillFeedEmbed Exception");
            }
        }
    }
}
