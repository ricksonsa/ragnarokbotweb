using Discord;
using Discord.Rest;
using Discord.WebSocket;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Interfaces;

namespace RagnarokBotWeb.Application.Discord;

public class DiscordCreateChannel(DiscordSocketClient client, IServiceProvider serviceProvider)
{
    public async Task<List<ChannelDto>> CreateAsync(ulong guildDiscordId)
    {
        var channels = new List<ChannelDto>();

        var guild = client.GetGuild(guildDiscordId);
        var templates = await GetChannelTemplates();
        var categories = new Dictionary<string, RestCategoryChannel>();

        foreach (var template in templates)
        {
            var category = await GetOrCreateCategoryChannelAsync(guild, template, categories);
            var channel = await guild.CreateTextChannelAsync(template.Name, props =>
            {
                props.CategoryId = category.IsSpecified ? category.Value.Id : null;
                props.PermissionOverwrites = BuildPermissionOverwrites(guild, template.Admin);
            });
            channels.Add(new ChannelDto(channel.Id, template.ChannelType));
        }

        return channels;
    }

    private static async Task<Optional<RestCategoryChannel>> GetOrCreateCategoryChannelAsync(
        SocketGuild guild,
        ChannelTemplate template,
        Dictionary<string, RestCategoryChannel> categories
    )
    {
        if (template.CategoryName == null) return Optional.Create<RestCategoryChannel>();
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