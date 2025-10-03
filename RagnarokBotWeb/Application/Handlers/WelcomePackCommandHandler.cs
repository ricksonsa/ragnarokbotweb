using RagnarokBotWeb.Application.Discord;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Application.Resolvers;
using RagnarokBotWeb.Domain.Exceptions;
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
        private readonly SteamAccountResolver _steamAccountResolver;

        public WelcomePackCommandHandler(
            IPlayerRepository playerRepository,
            IPlayerRegisterRepository playerRegisterRepository,
            IDiscordService discordService,
            IOrderService orderService,
            SteamAccountResolver steamAccountResolver)
        {
            _playerRepository = playerRepository;
            _playerRegisterRepository = playerRegisterRepository;
            _discordService = discordService;
            _orderService = orderService;
            _steamAccountResolver = steamAccountResolver;
        }

        public async Task ExecuteAsync(ChatTextParseResult input)
        {
            Match match = Regex.Match(input.Text, @"\d+");
            if (!match.Success) throw new DomainException($"Invalid welcompack number {input.Text}");

            string numericValue = match.Value;
            var register = await _playerRegisterRepository.FindOneWithServerByWelcomeIdAsync(numericValue);
            if (register is null) throw new DomainException($"Register not found for welcompack number {numericValue}");

            var player = await _playerRepository.FindOneWithServerAsync(p => p.SteamId64 == input.SteamId && p.ScumServerId == register.ScumServer.Id);
            if (player?.DiscordId != null) throw new DomainException($"Player with welcompack number {numericValue} already registered");

            player ??= new();
            player.SteamId64 = input.SteamId;

            var steamInfo = await _steamAccountResolver.Resolve(input.SteamId);
            if (steamInfo is not null)
            {
                player.SteamName = steamInfo.PersonaName;
                player.VacBan = steamInfo.VacBanned;
                player.VacBanCount = steamInfo.NumberOfVacBans;
            }

            player.Name = input.PlayerName;
            player.DiscordName = (await _discordService.GetDiscordUser(register.ScumServer.Guild!.DiscordId, register.DiscordId))?.DisplayName;
            player.DiscordId = register.DiscordId;
            player.ScumServer = register.ScumServer;
            player.WelcomePackClaimed = true;

            if (register.ScumServer.WelcomePackCoinAward > 0)
                player.Coin += register.ScumServer.WelcomePackCoinAward;

            await _playerRepository.CreateOrUpdateAsync(player);
            await _playerRepository.SaveAsync();

            register.Status = Domain.Enums.EPlayerRegisterStatus.Registered;
            await _playerRegisterRepository.CreateOrUpdateAsync(register);
            await _playerRegisterRepository.SaveAsync();

            string text = $"You are registered at {register.ScumServer.Name}.";
            var order = await _orderService.PlaceWelcomePackOrder(player);

            if (order != null)
                text += $" Stay put to receive your Welcome Pack {DiscordEmoji.Gift}";

            var embed = new CreateEmbed
            {
                GuildId = register.ScumServer.Guild!.DiscordId,
                DiscordId = register.DiscordId,
                Title = "Registration",
                Text = text,
            };
            embed.AddField(new CreateEmbedField("Server", register.ScumServer.Name!));

            await _discordService.SendEmbedToUserDM(embed);
        }
    }
}
