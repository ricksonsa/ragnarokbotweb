using Discord;
using Discord.WebSocket;
using RagnarokBotWeb.Application.Discord.Handlers;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Application.Discord.Events.Messages
{
    public class WalletBalanceEvent : IMessageEventHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public WalletBalanceEvent(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task HandleAsync(SocketMessage message)
        {
            throw new NotImplementedException();
        }

        public async Task HandleAsync(SocketMessageComponent message)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var playerRepository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();

                var player = await playerRepository
                    .FindOneWithServerAsync(u => u.ScumServer.Guild != null
                        && u.ScumServer.Guild.DiscordId == message.GuildId!.Value
                        && u.DiscordId == message.User.Id);

                Embed embed;

                if (player is not null)
                {
                    embed = new EmbedBuilder()
                        .WithTitle("Balance")
                        .WithDescription($"Your current coin balance: {player.Coin}")
                        .WithColor(Color.Green)
                        .WithCurrentTimestamp()
                        .Build();
                }
                else
                {
                    embed = new EmbedBuilder()
                        .WithTitle("Balance")
                        .WithDescription($"You are not yet registered. Please register first at #welcomepack")
                        .WithColor(Color.Red)
                        .WithCurrentTimestamp()
                        .Build();
                }

                await message.RespondAsync(embed: embed, ephemeral: true);
            }
            catch (Exception ex)
            {
                var embed = new EmbedBuilder()
                       .WithTitle("Balance")
                       .WithDescription($"Something went wrong, please try again later.")
                       .WithColor(Color.Red)
                       .WithCurrentTimestamp()
                       .Build();
                await message.RespondAsync(embed: embed, ephemeral: true);
                throw;
            }

        }
    }
}
