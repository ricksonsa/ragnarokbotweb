using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Handlers;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Discord.Events.Messages
{
    public class TaxiTeleportSelectEvent : IMessageEventHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public TaxiTeleportSelectEvent(IServiceProvider serviceProvider)
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
            DiscordEventService.UserTaxiTeleportSelections[(component.User.Id, component.GuildId.Value)] = selected;
            //await component.RespondAsync($"Taxi destination selected, please confirm and enjoy the ride", ephemeral: true);
            await component.DeferAsync(ephemeral: true);
        }
    }
}
