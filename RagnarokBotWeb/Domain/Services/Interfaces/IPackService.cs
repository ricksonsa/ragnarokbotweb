using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Dto;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IPackService
    {
        Task<Page<PackDto>> GetPacksPageByFilterAsync(Paginator paginator, string? filter);
        Task<PackDto> FetchPackById(long id);
        Task<PackDto> CreatePackAsync(PackDto createPack);
        Task<PackDto> UpdatePackAsync(long id, PackDto createPack);
        Task<Pack> DeletePackAsync(long id);
        Task<PackDto> FetchWelcomePack();
    }
}
