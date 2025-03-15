using Discord;
using Discord.Rest;
using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Dto;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Application.Discord;

public class DiscordCreateChannel(DiscordSocketClient client, IServiceProvider serviceProvider)
{
    public async Task<List<ChannelDto>> CreateAsync(ulong guildDiscordId)
    {
        var channels = new List<ChannelDto>();

        var guild = client.GetGuild(guildDiscordId);
        var channelTemplates = await GetChannelTemplates();
        var categories = new Dictionary<string, RestCategoryChannel>();

        foreach (var channelTemplate in channelTemplates)
        {
            var category = await GetOrCreateCategoryChannelAsync(guild, channelTemplate, categories);
            var channel = await CreateTextChannelAsync(guild, channelTemplate, category);
            var channelDto = new ChannelDto(channel.Id, channelTemplate.ChannelType);

            if (channelTemplate.Buttons is not null)
                foreach (var buttonTemplate in channelTemplate.Buttons)
                {
                    await CreateButtonAsync(channel, buttonTemplate);
                    channelDto.Buttons.Add(new ButtonDto(buttonTemplate.Command, buttonTemplate.Name));
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
            props.PermissionOverwrites = BuildPermissionOverwrites(guild, template.Admin);
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

    private static Optional<IEnumerable<Overwrite>> BuildPermissionOverwrites(SocketGuild guild, bool adminOnly = false)
    {
        var allAdminRoles = guild.Roles.Where(x => x.Permissions.Administrator).ToList();

        var everyonePerms = new OverwritePermissions(
            viewChannel: adminOnly ? PermValue.Deny : PermValue.Allow,
            sendMessages: PermValue.Deny
        );

        var adminPerms = new OverwritePermissions(
            viewChannel: PermValue.Allow,
            sendMessages: PermValue.Allow
        );

        var overwrites = allAdminRoles.Select(x => new Overwrite(x.Id, PermissionTarget.Role, adminPerms)).ToList();
        overwrites.Add(new Overwrite(guild.EveryoneRole.Id, PermissionTarget.Role, everyonePerms));
        return overwrites;
    }

    private async Task<IEnumerable<ChannelTemplate>> GetChannelTemplates()
    {
        using var scope = serviceProvider.CreateScope();
        var channelTemplateService = scope.ServiceProvider.GetRequiredService<IChannelTemplateService>();
        return await channelTemplateService.GetAllAsync();
    }
}