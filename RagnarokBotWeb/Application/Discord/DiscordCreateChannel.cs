using Discord;
using Discord.Rest;
using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Dto;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Application.Discord;

public class DiscordCreateChannel(DiscordSocketClient client, IChannelTemplateService channelTemplateService, IDiscordService discordService)
{
    public async Task<List<ChannelDto>> CreateAsync(ScumServer server)
    {
        var channels = new List<ChannelDto>();

        var guild = client.GetGuild(server.Guild!.DiscordId);

        if (!guild.IsConnected) throw new AppNotInstalledException(server.Guild.DiscordId);

        var channelTemplates = await GetChannelTemplates();
        var categories = new Dictionary<string, RestCategoryChannel>();

        foreach (var channelTemplate in channelTemplates)
        {
            var category = await GetOrCreateCategoryChannelAsync(guild, channelTemplate, categories);
            var channel = await CreateTextChannelAsync(guild, channelTemplate, category);
            var channelDto = new ChannelDto(channel.Id, ChannelTemplateValue.FromValue(channelTemplate.ChannelType));

            if (channelTemplate.Buttons is not null)
                foreach (var buttonTemplate in channelTemplate.Buttons)
                {
                    if (buttonTemplate.Command == "uav_scan_trigger")
                    {
                        server.Uav ??= new();
                        server.Uav.DiscordMessageId = (await discordService.CreateUavButtons(server, channel.Id))?.Id;
                    }
                    else
                    {
                        var message = await CreateButtonAsync(channel, buttonTemplate);
                        channelDto.Buttons.Add(new ButtonDto(buttonTemplate.Command, buttonTemplate.Name, message.Id));
                    }

                }

            channels.Add(channelDto);
        }

        return channels;
    }

    private static Task<RestTextChannel> CreateTextChannelAsync(SocketGuild guild, ChannelTemplate template,
        RestCategoryChannel? category)
    {
        return guild.CreateTextChannelAsync(template.Name, props =>
        {
            props.CategoryId = category?.Id;
            props.PermissionOverwrites = DiscordSocketClientUtils.BuildPermissionOverwrites(guild, template.Admin);
        });
    }

    private static Task<RestUserMessage> CreateButtonAsync(RestTextChannel channel, ButtonTemplate buttonTemplate)
    {
        var button = new ButtonBuilder()
            .WithLabel(buttonTemplate.Name)
            .WithCustomId(buttonTemplate.Command)
            .WithStyle(ButtonStyle.Primary);

        var messageComponent = new ComponentBuilder()
            .WithButton(button)
            .Build();

        return channel.SendMessageAsync(components: messageComponent);
    }

    private static async Task<RestCategoryChannel?> GetOrCreateCategoryChannelAsync(SocketGuild guild,
        ChannelTemplate template, Dictionary<string, RestCategoryChannel> categories)
    {
        if (template.CategoryName == null) return null;
        if (categories.TryGetValue(template.CategoryName, out var c)) return c;

        var category = await guild.CreateCategoryChannelAsync(template.CategoryName);
        categories[template.CategoryName] = category;
        return category;
    }

    private async Task<IEnumerable<ChannelTemplate>> GetChannelTemplates()
    {
        return await channelTemplateService.GetAllAsync();
    }
}