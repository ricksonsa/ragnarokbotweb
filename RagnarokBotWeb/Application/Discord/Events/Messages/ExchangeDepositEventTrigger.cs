using Discord;
using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Handlers;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Discord.Events.Messages
{
    public class ExchangeDepositEventTrigger : IMessageEventHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public ExchangeDepositEventTrigger(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task HandleAsync(SocketMessage message)
        {
            throw new NotImplementedException();
        }

        public async Task HandleAsync(SocketMessageComponent message)
        {
            using var scope = _serviceProvider.CreateScope();
            var scumRepository = scope.ServiceProvider.GetRequiredService<IScumServerRepository>();
            var server = await scumRepository.FindByGuildId(message.GuildId!.Value);

            if (server?.Exchange is null)
            {
                var embed = new EmbedBuilder()
                .WithTitle("Error")
                .WithDescription("Exchange is disabled.")
                .WithColor(Color.Red)
                .Build();
                await message.RespondAsync(embed: embed, ephemeral: true);
                return;
            }

            if (!server.Exchange.AllowWithdraw)
            {
                var embed = new EmbedBuilder()
                .WithTitle("Error")
                .WithDescription("Exchange deposit is disabled.")
                .WithColor(Color.Red)
                .Build();
                await message.RespondAsync(embed: embed, ephemeral: true);
                return;
            }

            var modal = new ModalBuilder()
                .WithTitle("Deposit")
                .WithCustomId("deposit_modal")
                .AddTextInput("Amount", "deposit_amount", TextInputStyle.Paragraph, placeholder: "Enter the amount to transfer from in-game money to coins", required: true);

            await message.RespondWithModalAsync(modal.Build());
        }

        public Task HandleAsync(SocketModal message)
        {
            throw new NotImplementedException();
        }
    }
}
