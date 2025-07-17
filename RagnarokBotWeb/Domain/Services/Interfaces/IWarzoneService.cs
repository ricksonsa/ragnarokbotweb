using RagnarokBotWeb.Application.Pagination;
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
    }
}
