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
        private readonly IPlayerRepository _playerRepository;

        public WelcomePackCommandHandler(
            IPlayerRepository playerRepository,
            IPlayerRegisterRepository playerRegisterRepository,
            IDiscordService discordService)
        {
            _playerRepository = playerRepository;
            _playerRegisterRepository = playerRegisterRepository;
            _discordService = discordService;
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
                player ??= new();
                player.SteamId64 = input.SteamId;
                player.SteamName = await new SteamAccountResolver().Resolve(input.SteamId);
                player.DiscordName = _discordService.GetDiscordUserName(register.DiscordId);
                player.DiscordId = register.DiscordId;
                player.ScumServer = register.ScumServer;

                await _playerRepository.CreateOrUpdateAsync(player);
                await _playerRepository.SaveAsync();

                await _discordService.SendEmbedToUserDM(new CreateEmbed
                {
                    DiscordId = register.DiscordId,
                    Text = $"Você foi registrado no servidor {register.ScumServer.Name}, aguarde no local para receber seu Welcome Pack {DiscordEmoji.Gift}",
                });
            }
            else
            {
                Console.WriteLine("No numeric value found.");
            }
        }
    }
}
