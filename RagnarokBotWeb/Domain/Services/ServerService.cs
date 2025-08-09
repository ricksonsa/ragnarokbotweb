using AutoMapper;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Discord;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;
using System.Text;

namespace RagnarokBotWeb.Domain.Services
{
    public class ServerService : BaseService, IServerService
    {
        private readonly ILogger<ServerService> _logger;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IGuildRepository _guildRepository;
        private readonly ITaskService _taskService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFtpService _ftpService;
        private readonly IMapper _mapper;
        private readonly DiscordSocketClient _discordClient;
        private readonly IDiscordService _discordService;
        private readonly StartupDiscordTemplate _startupDiscordTemplate;
        private readonly IChannelTemplateRepository _channelTemplateRepository;
        private readonly IChannelRepository _channelRepository;
        private readonly ICacheService _cacheService;
        private readonly IFileService _fileService;

        public ServerService(
            IHttpContextAccessor httpContext,
            ILogger<ServerService> logger,
            IScumServerRepository scumServerRepository,
            ITenantRepository tenantRepository,
            IUnitOfWork unitOfWork,
            IFtpService ftpService,
            IMapper mapper,
            ITaskService taskService,
            IGuildRepository guildRepository,
            DiscordSocketClient discordClient,
            IDiscordService discordService,
            IChannelTemplateRepository channelTemplateRepository,
            IChannelRepository channelRepository,
            ICacheService cacheService,
            StartupDiscordTemplate startupDiscordTemplate,
            IFileService fileService) : base(httpContext)
        {
            _logger = logger;
            _scumServerRepository = scumServerRepository;
            _tenantRepository = tenantRepository;
            _unitOfWork = unitOfWork;
            _ftpService = ftpService;
            _mapper = mapper;
            _taskService = taskService;
            _guildRepository = guildRepository;
            _discordClient = discordClient;
            _discordService = discordService;
            _channelTemplateRepository = channelTemplateRepository;
            _channelRepository = channelRepository;
            _cacheService = cacheService;
            _startupDiscordTemplate = startupDiscordTemplate;
            _fileService = fileService;
        }

        public async Task<ScumServerDto> ChangeFtp(FtpDto ftpDto)
        {
            var serverId = ServerId();

            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            if (server is null) throw new DomainException("ScumServer not found");

            string rootPath;
            try
            {
                var files = await new FtpScanner($"{ftpDto.Address}", ftpDto.UserName, ftpDto.Password).FindServerSettingsFilesAsync("SCUM");
                if (files is null || files.Count == 0) throw new DomainException("Invalid ftp server");
                rootPath = files.FirstOrDefault()!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw new DomainException("Invalid ftp server");
            }

            var newFtp = new Ftp
            {
                Address = ftpDto.Address,
                Port = ftpDto.Port,
                UserName = ftpDto.UserName,
                Password = ftpDto.Password,
                Provider = ftpDto.Provider,
                RootFolder = rootPath
            };

            Dictionary<string, string> data = [];
            data.Add("ServerName", "");
            data.Add("MaxPlayers", "");
            await GetServerConfigLineValue(server.Ftp!, data);
            server.Name = data["ServerName"];
            server.Slots = int.Parse(data["MaxPlayers"]);

            if (server.Ftp is not null)
            {
                _unitOfWork.Ftps.Remove(server.Ftp);
                await _unitOfWork.SaveAsync();
            }
            server.Ftp = newFtp;

            await _scumServerRepository.CreateOrUpdateAsync(server);
            await _scumServerRepository.SaveAsync();

            await _taskService.FtpConfigAddedAsync(server);

            return _mapper.Map<ScumServerDto>(server);
        }

        public async Task UpdateServerData(ScumServer server)
        {
            if (server.Ftp is null) return;
            Dictionary<string, string> data = [];
            data.Add("MaxPlayers", "");
            data.Add("ServerName", "");
            await GetServerConfigLineValue(server.Ftp!, data);
            server.Name = data["ServerName"];
            server.Slots = int.Parse(data["MaxPlayers"]);
            await _scumServerRepository.CreateOrUpdateAsync(server);
            await _scumServerRepository.SaveAsync();
        }

        public async Task<List<PlayerDto>> GetOnlinePlayers()
        {
            var serverId = ServerId();

            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            if (server is null) return [];

            var onlinePlayers = _cacheService.GetConnectedPlayers(serverId.Value).ToList();
            var players = (await _unitOfWork.Players.Include(player => player.ScumServer)
                .Where(player => player.ScumServerId == serverId.Value)
                .ToListAsync()).Where(player => onlinePlayers.Any(op => op.SteamID == player.SteamId64));

            return players.Select(_mapper.Map<PlayerDto>).ToList();
        }

        public async Task GetServerConfigLineValue(Ftp ftp, Dictionary<string, string> data)
        {
            if (ftp is null) throw new DomainException("Invalid ftp server");
            try
            {
                var client = _ftpService.GetClient(ftp);
                using (var stream = client.OpenRead($@"{ftp.RootFolder}/Saved/Config/WindowsServer/ServerSettings.ini"))
                using (var reader = new StreamReader(stream, encoding: Encoding.UTF8))
                    while (await reader.ReadLineAsync() is { } line)
                        foreach (var item in data)
                            if (line.Contains($"scum.{item.Key}="))
                                data[item.Key] = line.Split("=")[1];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw new DomainException("Invalid FTP server");
            }
        }

        public async Task AddGuild(ChangeGuildDto guildDto)
        {
            var tenantId = TenantId()!;

            var tenant = await _tenantRepository.FindByIdAsync(tenantId.Value);
            if (tenant is null) throw new DomainException("Tenant not found");

            var server = await _scumServerRepository.FindOneByTenantIdAsync(tenantId.Value);
            if (server is null) throw new DomainException("ScumServer not found");

            if (server.Guild is not null) throw new DomainException("Server already has a guild");
            server.Guild = new Guild()
            {
                Enabled = true, // TODO: Verificar se pagamento está em dia
                DiscordId = guildDto.GuildId,
                RunTemplate = false,
            };

            await _scumServerRepository.CreateOrUpdateAsync(server);
            await _scumServerRepository.SaveAsync();
        }

        public async Task<ScumServer> GetServer(long serverId)
        {
            var server = await _scumServerRepository.FindByIdAsync(serverId);
            if (server is null) throw new NotFoundException("Server not found");
            if (!server.Tenant.Enabled) throw new DomainException("Server tenant is not avaiable");
            return server;
        }

        public async Task<ScumServer> GetServer()
        {
            var serverId = ServerId()!;
            var server = await _scumServerRepository.FindActiveById(serverId.Value);
            if (server is null) throw new NotFoundException("Server not found");

            if (!server.Tenant.Enabled) throw new DomainException("Server tenant is not avaiable");
            return server;
        }

        public async Task<GuildDto> ConfirmDiscordToken(SaveDiscordSettingsDto settings)
        {
            var serverId = ServerId()!;

            var server = await _scumServerRepository.FindActiveById(serverId.Value);
            if (server is null) throw new NotFoundException("Server not found");

            if (settings.Token is null && (server.Guild is null || !server.Guild.Confirmed)) throw new DomainException("Invalid discord confirmation token");

            if (string.IsNullOrEmpty(settings.Token) && server.Guild?.Token != null)
            {
                settings.Token = server.Guild.Token;
            }

            var guild = await _guildRepository.FindOneWithScumServerAsync(g => g.Token == settings.Token);
            string discordId = settings.Token[(settings.Token.LastIndexOf('-') + 1)..];

            if (!ulong.TryParse(discordId, out ulong id))
                throw new NotFoundException("The token provided is invalid");

            var socketGuild = _discordClient.Guilds.FirstOrDefault(g => g.Id == id);

            if (socketGuild is null)
                throw new NotFoundException("The token provided is invalid");

            guild ??= new Guild();
            guild.Token = settings.Token;
            guild.DiscordId = id;
            guild.DiscordName = socketGuild.Name;
            guild.Enabled = true;
            guild.Confirmed = true;

            if (guild.ScumServer is null) guild.ScumServer = server;
            if (settings.DiscordLink is not null) guild.DiscordLink = settings.DiscordLink;

            await _guildRepository.CreateOrUpdateAsync(guild);
            await _guildRepository.SaveAsync();

            return _mapper.Map<GuildDto>(guild);
        }

        public async Task<GuildDto> GetServerDiscord()
        {
            var serverId = ServerId()!;

            var guild = await _guildRepository.FindOneWithScumServerAsync(g => g.ScumServer.Id == serverId.Value);
            if (guild is null) throw new DomainException("Server does not have a discord configured");

            var channels = _discordClient.GetGuild(guild.DiscordId).Channels;

            var guildDto = _mapper.Map<GuildDto>(guild);

            guildDto.Channels = channels
                .Where(channel => channel.ChannelType == Discord.ChannelType.Text)
                .Select(channel => new Dto.ChannelDto { DiscordId = channel.Id.ToString(), Name = channel.Name })
                .Reverse()
                .ToList();

            return guildDto;
        }

        public async Task<List<DiscordRolesDto>> GetServerDiscordRoles()
        {
            var serverId = ServerId()!;

            var guild = await _guildRepository.FindOneWithScumServerAsync(g => g.ScumServer.Id == serverId.Value);
            if (guild is null) throw new DomainException("Server does not have a discord configured");

            var roles = _discordClient.GetGuild(guild.DiscordId).Roles;

            return roles
                .Where(role => role.Name != "@everyone" && role.Name != "The SCUM Bot")
                .Select(role => new DiscordRolesDto { DiscordId = role.Id.ToString(), Name = role.Name })
                .ToList();
        }

        public async Task<GuildDto> RunDiscordTemplate()
        {
            var serverId = ServerId()!;

            var server = await _scumServerRepository.FindActiveById(serverId.Value);
            if (server is null) throw new NotFoundException("Server not found");
            if (server.Guild is null) throw new NotFoundException("Server does not have a discord set");

            await _startupDiscordTemplate.Run(server);
            server.Guild.RunTemplate = true;

            await _scumServerRepository.CreateOrUpdateAsync(server);
            await _scumServerRepository.SaveAsync();
            return _mapper.Map<GuildDto>(server.Guild);
        }

        public async Task<ScumServerDto?> SaveServerDiscordChannel(SaveChannelDto saveChannel)
        {
            var serverId = ServerId()!;
            var server = await _scumServerRepository.FindActiveById(serverId.Value);
            if (server is null) throw new NotFoundException("Server not found");

            if (server.Guild is null) throw new NotFoundException("Discord not configured");


            ulong? discordId = null;
            if (ulong.TryParse(saveChannel.Value, out ulong result))
                discordId = result;

            var channel = await _channelRepository.FindOneByServerIdAndChatType(serverId.Value, saveChannel.Key) ?? new();

            if (!discordId.HasValue)
            {
                if (!channel.IsTransitory())
                {
                    if (channel.Buttons?.Count > 0 && channel.DiscordId != discordId)
                        foreach (var button in channel.Buttons)
                            await _discordService.RemoveMessage(channel.DiscordId, button.MessageId);

                    _channelRepository.Delete(channel);
                    await _channelRepository.SaveAsync();
                }
                return _mapper.Map<ScumServerDto>(server);
            }

            var channelTemplateValue = ChannelTemplateValue.FromValue(saveChannel.Key);
            if (channelTemplateValue is null)
            {
                _logger.LogError("Invalid template channelType[{Key}]", saveChannel.Key);
                throw new Exception("Invalid template channelType");
            }

            channel.Guild = server.Guild!;
            channel.ChannelType = channelTemplateValue.ToString();
            channel.DiscordId = discordId!.Value;

            var channelTemplate = await _channelTemplateRepository.FindOneAsync(ct => ct.ChannelType == channel.ChannelType);
            if (channelTemplate is null)
            {
                _logger.LogError("Channel template not found with channelType[{Key}]", channel.ChannelType);
                throw new Exception("Channel template not found with channelType");
            }

            if (channelTemplate.Buttons is not null)
                foreach (var buttonTemplate in channelTemplate.Buttons)
                {
                    IUserMessage? message = null;
                    if (buttonTemplate.Command == "uav_scan_trigger")
                    {
                        server.Uav ??= new();
                        message = await _discordService.CreateUavButtons(server, channel.DiscordId);
                        server.Uav.DiscordMessageId = message?.Id;
                    }
                    else
                    {
                        message = await _discordService.CreateButtonAsync(channel.DiscordId, buttonTemplate);
                    }

                    if (message is null)
                    {
                        _logger.LogError("Coudn't create discord button [{Button}] of template [{Template}] on discord channel with id [{Id}]", buttonTemplate.Name, buttonTemplate.ChannelTemplate.Name, channel.DiscordId);
                        continue;
                    }
                    var button = new Button(buttonTemplate.Command, buttonTemplate.Name, message.Id);
                    channel.Buttons?.Add(button);
                }

            await _channelRepository.CreateOrUpdateAsync(channel);
            await _channelRepository.SaveAsync();


            return _mapper.Map<ScumServerDto>(server);
        }

        public async Task<UavDto> UpdateUav(UavDto dto)
        {
            var serverId = ServerId()!;
            var server = await _scumServerRepository.FindActiveById(serverId.Value);
            if (server is null) throw new NotFoundException("Server not found");

            ValidateSubscription(server);

            var previousImage = server.Uav?.ImageUrl;
            var previousDiscordChannel = server.Uav?.DiscordId;
            var previousDiscordMessage = server.Uav?.DiscordMessageId;
            server.Uav = _mapper.Map(dto, server.Uav ?? new Uav());
            if (dto.DiscordId != null) server.Uav.DiscordId = ulong.Parse(dto.DiscordId);

            if (!string.IsNullOrEmpty(server.Uav.ImageUrl) && server.Uav.ImageUrl != previousImage)
            {
                if (!string.IsNullOrEmpty(previousImage)) _fileService.DeleteFile(previousImage);
                server.Uav.ImageUrl = await _fileService.SaveCompressedBase64ImageAsync(server.Uav.ImageUrl);
            }

            try
            {
                if (previousDiscordChannel.HasValue && previousDiscordMessage.HasValue)
                    await _discordService.RemoveMessage(previousDiscordChannel.Value, previousDiscordMessage.Value);

                if (server.Uav.DiscordId.HasValue)
                    server.Uav.DiscordMessageId = (await _discordService.CreateUavButtons(server, server.Uav.DiscordId.Value))?.Id;
            }
            catch (Exception)
            { }

            await _scumServerRepository.CreateOrUpdateAsync(server);
            await _scumServerRepository.SaveAsync();
            return _mapper.Map<UavDto>(server.Uav);
        }

        public async Task<List<SaveChannelDto>> GetServerDiscordChannels()
        {
            var serverId = ServerId()!;
            var server = await _scumServerRepository.FindActiveById(serverId.Value);
            if (server is null) throw new NotFoundException("Server not found");

            if (server.Guild is null) throw new NotFoundException("Discord not configured");
            var channels = await _channelRepository.FindAllByServerId(serverId.Value);
            return channels.Select(channel => new SaveChannelDto { Key = channel.ChannelType!, Value = channel.DiscordId.ToString() }).ToList();
        }

        public async Task<ScumServerDto> UpdateServerSettings(UpdateServerSettingsDto updateServer)
        {
            var serverId = ServerId()!;
            var server = await _scumServerRepository.FindActiveById(serverId.Value);
            if (server is null) throw new NotFoundException("Server not found");

            server = _mapper.Map(updateServer, server);

            if (!server.IsCompliant())
            {
                server.ShowKillOnMap = false;
                server.ShowSameSquadKill = true;
                server.AllowMinesOutsideFlag = true;
            }

            await _scumServerRepository.CreateOrUpdateAsync(server);
            await _scumServerRepository.SaveAsync();

            return _mapper.Map<ScumServerDto>(server);
        }

        public async Task<ScumServerDto> UpdateKillFeed(UpdateKillFeedDto updateKillFeed)
        {
            var serverId = ServerId()!;
            var server = await _scumServerRepository.FindActiveById(serverId.Value);
            if (server is null) throw new NotFoundException("Server not found");

            server = _mapper.Map(updateKillFeed, server);

            if (!server.IsCompliant())
            {
                server.ShowKillOnMap = false;
                server.ShowSameSquadKill = true;
                server.AllowMinesOutsideFlag = true;
            }

            await _scumServerRepository.CreateOrUpdateAsync(server);
            await _scumServerRepository.SaveAsync();

            return _mapper.Map<ScumServerDto>(server);
        }

    }
}
