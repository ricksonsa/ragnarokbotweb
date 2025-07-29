using Discord;
using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Domain.Services
{
    public class DiscordService : IDiscordService
    {
        private readonly ILogger<DiscordService> _logger;
        private readonly DiscordSocketClient _client;
        private readonly IGuildService _guildService;
        private readonly StartupDiscordTemplate _startupDiscordTemplate;
        private readonly IChannelService _channelService;

        public DiscordService(
            ILogger<DiscordService> logger,
            DiscordSocketClient client,
            IGuildService guildService,
            StartupDiscordTemplate startupDiscordTemplate,
            IChannelService channelService)
        {
            _logger = logger;
            _client = client;
            _guildService = guildService;
            _startupDiscordTemplate = startupDiscordTemplate;
            _channelService = channelService;
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
            var user = _client.GetUser(createEmbed.DiscordId);
            if (user is null)
            {
                _logger.LogInformation("User with discordId: {DiscordId} was not found", createEmbed.DiscordId);
                return;
            }

            var dmChannel = await user.CreateDMChannelAsync();

            var embed = new EmbedBuilder()
                .WithTitle(createEmbed.Title)
                .WithDescription(createEmbed.Text)
                .WithImageUrl(createEmbed.ImageUrl)
                .WithColor(Color.Blue)
                .Build();

            var builder = new ComponentBuilder();

            createEmbed.Buttons.ForEach(button =>
            {
                builder.WithButton(
                    label: button.Label,
                    customId: button.ActionId,
                    style: ButtonStyle.Primary);
            });

            await dmChannel.SendMessageAsync(embed: embed, components: builder.Build());
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
                    .WithDescription(createEmbed.Text)
                    .WithThumbnailUrl("attachment://" + filename)
                    .WithImageUrl("attachment://" + filename)
                    .WithColor(Color.Blue)
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

        public async Task<IUserMessage> SendEmbedToChannel(CreateEmbed createEmbed)
        {
            var channel = _client.GetChannel(createEmbed.DiscordId) as IMessageChannel;

            if (channel != null)
            {
                var embed = new EmbedBuilder()
                    .WithTitle(createEmbed.Title)
                    .WithDescription(createEmbed.Text)
                    .WithImageUrl(createEmbed.ImageUrl)
                    .WithColor(Color.Blue)
                    .Build();

                var builder = new ComponentBuilder();

                createEmbed.Buttons.ForEach(button =>
                {
                    builder.WithButton(
                        label: button.Label,
                        customId: button.ActionId,
                        style: ButtonStyle.Primary);
                });

                return await channel.SendMessageAsync(embed: embed, components: builder.Build());
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
    }
}
