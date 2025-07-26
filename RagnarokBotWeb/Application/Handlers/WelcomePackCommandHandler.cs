using RagnarokBotWeb.Application.Discord;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Application.Resolvers;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using System.Text.RegularExpressions;

namespace RagnarokBotWeb.Application.Handlers
{
    public class WelcomePackCommandHandler : IExclamationCommandHandler
    {
        private readonly IPlayerRegisterRepository _playerRegisterRepository;
        private readonly IDiscordService _discordService;
        private readonly IOrderService _orderService;
        private readonly IPlayerRepository _playerRepository;

        public WelcomePackCommandHandler(
            IPlayerRepository playerRepository,
            IPlayerRegisterRepository playerRegisterRepository,
            IDiscordService discordService,
            IOrderService orderService)
        {
            _playerRepository = playerRepository;
            _playerRegisterRepository = playerRegisterRepository;
            _discordService = discordService;
            _orderService = orderService;
        }

        public async Task ExecuteAsync(ChatTextParseResult input)
        {
            // Extract digits using regex
            Match match = Regex.Match(input.Text, @"\d+");
            if (match.Success)
            {
                string numericValue = match.Value;
                var register = await _playerRegisterRepository.FindOneWithServerByWelcomeIdAsync(numericValue);
                if (register is null) return;

                var player = await _playerRepository.FindOneAsync(p => p.SteamId64 == input.SteamId);
                if (player?.DiscordId != null) return;

                player ??= new();
                player.SteamId64 = input.SteamId;
                player.SteamName = await new SteamAccountResolver().Resolve(input.SteamId);
                player.Name = input.PlayerName;
                player.DiscordName = _discordService.GetDiscordUserName(register.DiscordId);
                player.DiscordId = register.DiscordId;
                player.ScumServer = register.ScumServer;

                await _playerRepository.CreateOrUpdateAsync(player);
                await _playerRepository.SaveAsync();

                register.Status = Domain.Enums.EPlayerRegisterStatus.Registered;
                await _playerRegisterRepository.CreateOrUpdateAsync(register);
                await _playerRegisterRepository.SaveAsync();

                string text = $"You were registered at {register.ScumServer.Name}.";
                var order = await _orderService.PlaceWelcomePackOrder(player);
                if (order != null)
                {
                    text += $" Stay put to receive your Welcome Pack {DiscordEmoji.Gift}";
                }

                await _discordService.SendEmbedToUserDM(new CreateEmbed
                {
                    DiscordId = register.DiscordId,
                    Title = "Registration",
                    Text = text,
                });
            }
            else
            {
                Console.WriteLine("No numeric value found.");
            }
        }
    }
}
