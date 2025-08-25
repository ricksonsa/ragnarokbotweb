using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Handlers;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Discord.Events.Messages
{
    public class UavSelectEvent : IMessageEventHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public UavSelectEvent(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task HandleAsync(SocketMessage message)
        {
            throw new NotImplementedException();
        }

        public async Task HandleAsync(SocketMessageComponent component)
        {
            using var scope = _serviceProvider.CreateScope();
            var scumRepository = scope.ServiceProvider.GetRequiredService<IScumServerRepository>();
            var server = await scumRepository.FindByGuildId(component.GuildId!.Value);
            var selected = component.Data.Values.First();
            if (selected == "0") await component.DeferAsync(ephemeral: true);
            DiscordEventService.UserUavSelections[(component.User.Id, component.GuildId.Value)] = selected;
            await component.DeferAsync(ephemeral: true);
        }

        public Task HandleAsync(SocketModal message)
        {
            throw new NotImplementedException();
        }
    }
}
