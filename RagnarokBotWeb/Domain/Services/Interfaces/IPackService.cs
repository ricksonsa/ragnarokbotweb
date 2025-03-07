using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Dto;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IPackService
    {
        Task<IEnumerable<PackDto>> FetchAllPacksAsync();
        Task<PackDto> FetchPackById(long id);
        Task<PackDto> CreatePackAsync(PackDto createPack);
        Task<PackDto> UpdatePackAsync(long id, PackDto createPack);
        Task<Pack> DeletePackAsync(long id);
    }
}
