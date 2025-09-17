using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RagnarokBotWeb.Application.BotServer;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.HostedServices.Base;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using Shared.Models;
using Shared.Parser;

namespace RagnarokBotWeb.Domain.Services
{
    public class BotService : BaseService, IBotService
    {
        private readonly ILogger<BotService> _logger;
        private readonly ICacheService _cacheService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly BotSocketServer _botSocket;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDiscordService _discordService;

        public BotService(
            IHttpContextAccessor httpContext,
            ICacheService cacheService,
            ILogger<BotService> logger,
            IScumServerRepository scumServerRepository,
            IUnitOfWork unitOfWork,
            BotSocketServer botSocket,
            IServiceProvider serviceProvider,
            IDiscordService discordService) : base(httpContext)
        {
            _cacheService = cacheService;
            _logger = logger;
            _scumServerRepository = scumServerRepository;
            _unitOfWork = unitOfWork;
            _botSocket = botSocket;
            _serviceProvider = serviceProvider;
            _discordService = discordService;
        }

        public async Task UpdatePlayersOnline(UpdateFromStringRequest input)
        {
            var serverId = ServerId();
            var players = ListPlayersParser.Parse(input.Value);
            if (players == null || players.Count == 0) return;
            var squads = _cacheService.GetSquads(serverId!.Value);
            foreach (var player in players)
            {
                var squad = squads.FirstOrDefault(squad => squad.Members.Any(member => member.SteamId == player.SteamID));
                player.SquadName = squad?.SquadName;
                player.SquadId = squad?.SquadId;
            }
            _cacheService.SetConnectedPlayers(serverId!.Value, players);
            await UpdateFromScumPlayers(serverId.Value, players);
        }

        public async Task UpdateFromScumPlayers(long serverId, List<ScumPlayer>? players)
        {
            if (players is null) players = _cacheService.GetConnectedPlayers(serverId);

            using var scope = _serviceProvider.CreateScope(); // inject IServiceProvider
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var player in players)
            {
                try
                {
                    var user = await ctx.Players
                                    .Include(p => p.ScumServer)
                                    .Include(p => p.ScumServer.Guild)
                                    .FirstOrDefaultAsync(p => p.SteamId64 == player.SteamID && p.ScumServerId == serverId);

                    user ??= new();
                    user.X = player.X;
                    user.Y = player.Y;
                    user.Z = player.Z;
                    user.Name = player.Name;
                    user.SteamId64 = player.SteamID;
                    user.SteamName = player.SteamName;
                    user.Gold = player.GoldBalance;
                    user.Money = player.AccountBalance;
                    user.Fame = player.Fame;
                    user.LastLoggedIn = DateTime.UtcNow;
                    user.ScumServer ??= (await ctx.ScumServers.Include(server => server.Guild).FirstOrDefaultAsync(server => server.Id == serverId))!;

                    try
                    {
                        if (!string.IsNullOrEmpty(user.DiscordName) && user.DiscordId.HasValue && user.ScumServer.Guild is not null)
                        {
                            var discordUser = await _discordService.GetDiscordUser(user.ScumServer.Guild.DiscordId, user.DiscordId.Value);
                            user.DiscordName = discordUser?.DisplayName;
                        }
                    }
                    catch (Exception) { }

                    if (user.Id == 0)
                    {
                        await ctx.Players.AddAsync(user);
                    }
                    else
                    {
                        ctx.Players.Update(user);
                    }

                    await ctx.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        public async Task UpdateFlags(UpdateFromStringRequest input)
        {
            var serverId = ServerId();

            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            if (server == null) return;

            var flags = ListFlagsParser.Parse(input.Value);
            if (flags == null || flags.Count == 0) return;

            _cacheService.SetFlags(serverId.Value, flags);
            await new ScumFileProcessor(server, _unitOfWork).SaveFlagList(JsonConvert.SerializeObject(flags, Formatting.Indented));
        }

        public async Task UpdateSquads(UpdateFromStringRequest input)
        {
            var serverId = ServerId();

            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            if (server == null) return;

            var squads = ListSquadsParser.Parse(input.Value);
            if (squads == null || squads.Count == 0) return;

            _cacheService.SetSquads(serverId.Value, squads);
            await new ScumFileProcessor(server, _unitOfWork).SaveSquadList(JsonConvert.SerializeObject(squads, Formatting.Indented));
        }

        public async Task SendCommand(long serverId, BotCommand command)
        {
            if (command.Values.Any(cmd => cmd.CheckTargetOnline))
            {
                var players = _cacheService.GetConnectedPlayers(serverId);
                if (!players.Any(player => player.SteamID == command.Values.First(v => v.CheckTargetOnline).Target))
                {
                    _logger.LogInformation("Command received but player {Player} is not online, caching command", command.Values.First(v => v.CheckTargetOnline).Target);
                    _cacheService.EnqueueCommand(serverId, command);
                    return;
                }
            }

            await _botSocket.SendCommandAsync(serverId, command);
        }

        public bool IsBotOnline()
        {
            var serverId = ServerId();
            return _botSocket.IsBotConnected(serverId!.Value);
        }

        public bool IsBotOnline(long serverId)
        {
            return _botSocket.IsBotConnected(serverId);
        }

        public List<BotUser> FindActiveBotsByServerId(long serverId)
        {
            return _botSocket.GetBots(serverId).Where(bot => bot.LastPinged.HasValue).ToList();
        }

        public List<BotUser> GetBots(long serverId)
        {
            return _botSocket.GetBots(serverId);
        }

        public List<BotUser> GetBots()
        {
            var serverId = ServerId();
            return GetBots(serverId!.Value);
        }

        public List<BotUser> GetConnectedBots()
        {
            var serverId = ServerId();
            return _botSocket.GetBots(serverId!.Value).Where(bot => bot.LastPinged.HasValue).ToList();
        }

        public async Task ResetBotState(long serverId)
        {
            var now = DateTime.UtcNow;
            var bots = _botSocket.GetBots(serverId).Where(bot => bot.LastPinged.HasValue);
            foreach (var bot in bots)
            {
                var diff = (now - bot.LastPinged!.Value).TotalMinutes;
                if (diff >= 5)
                    await _botSocket.SendCommandAsync(serverId, bot.Guid.ToString(), new BotCommand().Reconnect());
            }
        }

        public async Task<BotUser?> FindBotByGuid(Guid guid)
        {
            var serverId = ServerId();
            var server = await _scumServerRepository.FindActiveById(serverId!.Value);

            ValidateSubscription(server!);

            if (!serverId.HasValue) throw new UnauthorizedAccessException();

            try
            {
                return _botSocket.GetBots(serverId.Value).FirstOrDefault(bot => bot.Guid == guid);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void BotPingUpdate(long id, Guid guid, string steamId)
        {
            _botSocket.BotPingUpdate(id, guid, steamId);
        }
    }
}
