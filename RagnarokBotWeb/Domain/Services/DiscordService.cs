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

        public async Task SendEmbed(CreateEmbed createEmbed)
        {
            var channel = _client.GetChannel(createEmbed.DiscordChannelId) as IMessageChannel;

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

                await channel.SendMessageAsync(embed: embed, components: builder.Build());
            }
        }
    }
}
