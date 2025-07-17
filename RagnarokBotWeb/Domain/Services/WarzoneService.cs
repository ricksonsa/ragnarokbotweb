using AutoMapper;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services
{
    public class WarzoneService : BaseService, IWarzoneService
    {
        private readonly ILogger<WarzoneService> _logger;
        private readonly IWarzoneRepository _warzoneRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IScumServerRepository _scumServerRepository;
        private readonly IMapper _mapper;

        public WarzoneService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<WarzoneService> logger,
            IWarzoneRepository warzoneRepository,
            IMapper mapper,
            IScumServerRepository scumServerRepository,
            IUnitOfWork unitOfWork) : base(httpContextAccessor)
        {
            _logger = logger;
            _warzoneRepository = warzoneRepository;
            _mapper = mapper;
            _scumServerRepository = scumServerRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<WarzoneDto> CreateWarzoneAsync(WarzoneDto createWarzone)
        {
            var serverId = ServerId();
            var warzone = _mapper.Map<Warzone>(createWarzone);

            var server = await _scumServerRepository.FindActiveById(serverId!.Value);
            if (server is null) throw new DomainException("Server tenant is not enabled");

            warzone.ScumServer = server;

            await _warzoneRepository.CreateOrUpdateAsync(warzone);
            await _warzoneRepository.SaveAsync();

            return _mapper.Map<WarzoneDto>(warzone);
        }

        public async Task<WarzoneDto> UpdateWarzoneAsync(long id, WarzoneDto createWarzone)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server id");

            var warzoneNotTracked = await _warzoneRepository.FindByIdAsNoTrackingAsync(id);
            if (warzoneNotTracked is null) throw new NotFoundException("Pack not found");

            if (warzoneNotTracked.ScumServer.Id != serverId.Value) throw new UnauthorizedException("Invalid server");

            if (warzoneNotTracked.WarzoneItems.Any())
            {
                warzoneNotTracked.WarzoneItems.ForEach(wi => wi.Item = null);
                _unitOfWork.WarzoneItems.RemoveRange(warzoneNotTracked.WarzoneItems);
            }

            if (warzoneNotTracked.Teleports.Any())
            {
                _unitOfWork.Teleports.RemoveRange(warzoneNotTracked.Teleports.Select(x => x.Teleport));
                _unitOfWork.WarzoneTeleports.RemoveRange(warzoneNotTracked.Teleports);
            }

            if (warzoneNotTracked.SpawnPoints.Any())
            {
                _unitOfWork.Teleports.RemoveRange(warzoneNotTracked.SpawnPoints.Select(x => x.Teleport));
                _unitOfWork.WarzoneSpawns.RemoveRange(warzoneNotTracked.SpawnPoints);
            }

            var warzone = _mapper.Map<Warzone>(createWarzone);
            warzone.ScumServer = warzoneNotTracked.ScumServer;

            await _warzoneRepository.CreateOrUpdateAsync(warzone);
            await _warzoneRepository.SaveAsync();

            return _mapper.Map<WarzoneDto>(warzone);
        }

        public async Task DeleteWarzoneAsync(long id)
        {
            var serverId = ServerId();
            if (!serverId.HasValue) throw new UnauthorizedException("Invalid server id");

            var warzoneNotTracked = await _warzoneRepository.FindByIdAsNoTrackingAsync(id);
            if (warzoneNotTracked is null) throw new NotFoundException("Package not found");
            if (warzoneNotTracked.ScumServer.Id != serverId.Value) throw new UnauthorizedException("Invalid server");

            if (warzoneNotTracked.WarzoneItems.Any())
            {
                warzoneNotTracked.WarzoneItems.ForEach(wi => wi.Item = null);
                _unitOfWork.WarzoneItems.RemoveRange(warzoneNotTracked.WarzoneItems);
            }

            if (warzoneNotTracked.Teleports.Any())
            {
                _unitOfWork.Teleports.RemoveRange(warzoneNotTracked.Teleports.Select(x => x.Teleport));
                _unitOfWork.WarzoneTeleports.RemoveRange(warzoneNotTracked.Teleports);
            }

            if (warzoneNotTracked.SpawnPoints.Any())
            {
                _unitOfWork.Teleports.RemoveRange(warzoneNotTracked.SpawnPoints.Select(x => x.Teleport));
                _unitOfWork.WarzoneSpawns.RemoveRange(warzoneNotTracked.SpawnPoints);
            }
            await _warzoneRepository.SaveAsync();

            var warzone = await _warzoneRepository.FindByIdAsync(id);
            warzone!.Deleted = DateTime.UtcNow;
            await _warzoneRepository.CreateOrUpdateAsync(warzoneNotTracked);
            await _warzoneRepository.SaveAsync();

            return;
        }

        public async Task<WarzoneDto> FetchWarzoneById(long id)
        {
            var serverId = ServerId();
            var warzone = await _warzoneRepository.FindByIdAsync(id);
            if (warzone is null) throw new NotFoundException("Warzone not found");

            if (warzone.ScumServer.Id != serverId!.Value) throw new UnauthorizedException("Invalid server id");

            return _mapper.Map<WarzoneDto>(warzone);
        }

        public async Task<Page<WarzoneDto>> GetWarzonePageByFilterAsync(Paginator paginator, string? filter)
        {
            var serverId = ServerId();
            var page = await _warzoneRepository.GetPageByServerAndFilter(paginator, serverId.Value, filter);
            return new Page<WarzoneDto>(page.Content.Select(_mapper.Map<WarzoneDto>), page.TotalPages, page.TotalElements, paginator.PageNumber, paginator.PageSize);
        }


    }
}
