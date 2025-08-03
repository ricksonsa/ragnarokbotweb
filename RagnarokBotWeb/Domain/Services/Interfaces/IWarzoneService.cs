using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Dto;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IWarzoneService
    {
        Task<Page<WarzoneDto>> GetWarzonePageByFilterAsync(Paginator paginator, string? filter);
        Task<WarzoneDto> FetchWarzoneById(long id);
        Task<WarzoneDto> CreateWarzoneAsync(WarzoneDto createWarzone);
        Task<WarzoneDto> UpdateWarzoneAsync(long id, WarzoneDto createWarzone);
        Task DeleteWarzoneAsync(long id);
        Task DeleteDiscordMessage(Warzone? warzone);
        Task<ulong> GenerateDiscordWarzoneButton(Warzone warzone);
        Task OpenWarzone(ScumServer server, CancellationToken token = default);
        Task CloseWarzone(ScumServer server, CancellationToken token = default);
        Task OpenWarzone();
        Task CloseWarzone();
    }
}
