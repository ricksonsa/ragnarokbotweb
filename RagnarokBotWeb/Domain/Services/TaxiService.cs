using AutoMapper;
using Discord;
using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Models;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services
{
    public class TaxiService : BaseService, ITaxiService
    {
        private readonly ILogger<TaxiService> _logger;
        private readonly ITaxiRepository _taxiRepository;
        private readonly IFileService _fileService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDiscordService _discordService;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly IMapper _mapper;

        public TaxiService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<TaxiService> logger,
            ITaxiRepository taxiRepository,
            IMapper mapper,
            IScumServerRepository scumServerRepository,
            IUnitOfWork unitOfWork,
            IDiscordService discordService,
            IFileService fileService) : base(httpContextAccessor)
        {
            _logger = logger;
            _taxiRepository = taxiRepository;
            _mapper = mapper;
            _scumServerRepository = scumServerRepository;
            _unitOfWork = unitOfWork;
            _discordService = discordService;
            _fileService = fileService;
        }

        public async Task<TaxiDto> CreateTaxiAsync(TaxiDto createTaxi)
        {
            var serverId = ServerId();
            var taxi = _mapper.Map<Taxi>(createTaxi);

            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            if (server is null) throw new DomainException("Server tenant is not enabled");

            ValidateSubscription(server);

            taxi.ScumServer = server;
            taxi.TaxiTeleports = createTaxi.TaxiTeleports.Select(_mapper.Map<TaxiTeleport>).ToList();

            if (!string.IsNullOrEmpty(taxi.ImageUrl))
                taxi.ImageUrl = await _fileService.SaveCompressedBase64ImageAsync(taxi.ImageUrl);

            try
            {
                if (!string.IsNullOrEmpty(taxi.DiscordChannelId))
                {
                    taxi.DiscordMessageId = await GenerateDiscordTaxiButton(taxi);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Taxi remove discord message exception");
            }

            await _taxiRepository.CreateOrUpdateAsync(taxi);
            await _taxiRepository.SaveAsync();

            return _mapper.Map<TaxiDto>(taxi);
        }

        public async Task<ulong> GenerateDiscordTaxiButton(Taxi taxi)
        {
            if (taxi.TaxiType == Enums.ETaxiType.RandomTeleport)
            {
                var action = $"buy_taxi:{taxi.Id}";
                var embed = new CreateEmbed
                {
                    Buttons = [new($"Buy {taxi.Name} Teleport", action)],
                    GuildId = taxi.ScumServer!.Guild!.DiscordId,
                    DiscordId = ulong.Parse(taxi.DiscordChannelId!),
                    Fields = GetFields(taxi),
                    Color = taxi.IsVipOnly ? Color.Gold : Color.DarkOrange,
                    Text = taxi.Description,
                    ImageUrl = taxi.ImageUrl,
                    Title = taxi.Name
                };

                IUserMessage message = await _discordService.SendEmbedToChannel(embed);
                return message.Id;
            }
            else
            {
                IUserMessage? message = await _discordService.CreateTeleportButtons(taxi);
                return message!.Id;
            }

        }

        private static List<CreateEmbedField> GetFields(Taxi taxi)
        {
            List<CreateEmbedField> fields = [];
            if (taxi.Price > 0) fields.Add(new CreateEmbedField("Price", taxi.Price.ToString(), true));
            if (taxi.VipPrice > 0) fields.Add(new CreateEmbedField("Vip Price", taxi.VipPrice.ToString(), true));
            return fields;
        }

        public async Task DeleteDiscordMessage(Taxi taxi)
        {
            await _discordService.RemoveMessage(ulong.Parse(taxi.DiscordChannelId!), taxi.DiscordMessageId!.Value);
        }

        public async Task<TaxiDto> UpdateTaxiAsync(long id, TaxiDto taxiDto)
        {
            var taxi = await _taxiRepository.FindByIdAsync(id);

            if (taxi == null)
                throw new NotFoundException("Taxi not found");

            ValidateServerOwner(taxi.ScumServer);
            ValidateSubscription(taxi.ScumServer);

            var previousImage = taxi.ImageUrl;
            var previousDiscordId = taxi.DiscordChannelId;
            var dicordMessageId = taxi.DiscordMessageId;
            _mapper.Map(taxiDto, taxi);

            if (!string.IsNullOrEmpty(taxi.ImageUrl) && taxi.ImageUrl != previousImage)
            {
                if (!string.IsNullOrEmpty(previousImage)) _fileService.DeleteFile(previousImage);
                taxi.ImageUrl = await _fileService.SaveCompressedBase64ImageAsync(taxi.ImageUrl);
            }

            RemoveTaxiTeleports(taxiDto, taxi);

            try
            {
                await _discordService.RemoveMessage(ulong.Parse(previousDiscordId!), dicordMessageId!.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Taxi remove discord message exception");
            }

            if (taxi.Enabled)
            {
                try
                {
                    taxi.DiscordMessageId = await GenerateDiscordTaxiButton(taxi);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Taxi update discord message exception");
                }
            }

            _unitOfWork.Taxis.Update(taxi);
            await _unitOfWork.SaveAsync();

            return _mapper.Map<TaxiDto>(taxi);
        }

        private void RemoveTaxiTeleports(TaxiDto taxiDto, Taxi existingTaxi)
        {
            var updatedItemIds = taxiDto.TaxiTeleports.Select(wi => wi.TeleportId).ToHashSet();

            foreach (var existingItem in existingTaxi.TaxiTeleports.ToList())
            {
                if (!updatedItemIds.Contains(existingItem.TeleportId))
                {
                    _unitOfWork.AppDbContext.TaxiTeleports.Remove(existingItem);
                }
            }

            foreach (var dto in taxiDto.TaxiTeleports)
            {
                if (!existingTaxi.TaxiTeleports.Any(wi => wi.TeleportId == dto.TeleportId))
                {
                    existingTaxi.TaxiTeleports.Add(new TaxiTeleport
                    {
                        Teleport = _mapper.Map<Teleport>(dto.Teleport),
                        TaxiId = existingTaxi.Id
                    });
                }
            }
        }

        public async Task DeleteTaxiAsync(long id)
        {
            var serverId = ServerId();
            await CheckAuthority(id, serverId);

            var taxi = await _unitOfWork.AppDbContext.Taxis
              .Include(w => w.TaxiTeleports)
                  .ThenInclude(w => w.Teleport)
              .FirstOrDefaultAsync(w => w.Id == id);

            if (taxi == null)
                throw new NotFoundException("Taxi not found");

            // Optionally clean up Discord message
            if (!string.IsNullOrEmpty(taxi.DiscordChannelId) && taxi.DiscordMessageId.HasValue)
            {
                try
                {
                    await _discordService.RemoveMessage(
                        ulong.Parse(taxi.DiscordChannelId!),
                        taxi.DiscordMessageId.Value
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to remove Discord message: {0}", ex.Message);
                }
            }

            // === Remove related entities ===

            _unitOfWork.AppDbContext.Teleports.RemoveRange(taxi.TaxiTeleports.Select(sp => sp.Teleport));
            //_unitOfWork.AppDbContext.TaxiTeleports.RemoveRange(taxi.TaxiTeleports);

            // === Remove main entity ===

            _unitOfWork.AppDbContext.Taxis.Remove(taxi);
            await _unitOfWork.AppDbContext.SaveChangesAsync();

            return;
        }

        private async Task CheckAuthority(long id, long? serverId)
        {
            if (!await _unitOfWork.AppDbContext.Taxis.AsNoTracking().Include(wz => wz.ScumServer).AnyAsync(wz => wz.ScumServer.Id == serverId && wz.Id == id))
            {
                throw new UnauthorizedException("Invalid taxi");
            }
        }

        public async Task<TaxiDto> FetchTaxiById(long id)
        {
            var taxi = await _taxiRepository.FindByIdAsync(id);
            if (taxi is null) throw new NotFoundException("Taxi not found");

            ValidateServerOwner(taxi.ScumServer);

            return _mapper.Map<TaxiDto>(taxi);
        }

        public async Task<Page<TaxiDto>> GetTaxisPageByFilterAsync(Paginator paginator, string? filter)
        {
            var serverId = ServerId();
            var page = await _taxiRepository.GetPageByServerAndFilter(paginator, serverId!.Value, filter);
            return new Page<TaxiDto>(page.Content.Select(_mapper.Map<TaxiDto>), page.TotalPages, page.TotalElements, paginator.PageNumber, paginator.PageSize);
        }
    }
}
