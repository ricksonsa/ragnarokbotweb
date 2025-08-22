using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Services.Dto;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface ITaxiService
    {
        Task<Page<TaxiDto>> GetTaxisPageByFilterAsync(Paginator paginator, string? filter);
        Task<TaxiDto> FetchTaxiById(long id);
        Task<TaxiDto> CreateTaxiAsync(TaxiDto createWarzone);
        Task<TaxiDto> UpdateTaxiAsync(long id, TaxiDto createWarzone);
        Task DeleteTaxiAsync(long id);
    }
}
