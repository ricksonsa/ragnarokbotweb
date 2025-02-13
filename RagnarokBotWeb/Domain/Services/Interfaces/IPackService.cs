using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Dto;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IPackService
    {
        Task<IEnumerable<PackDto>> FetchAllPacksAsync();
        Task<PackDto> FetchPackById(long id);
        Task<PackDto> CreatePackAsync(CreatePackDto createPack);
        Task<PackDto> UpdatePackAsync(long id, CreatePackDto createPack);
        Task<Pack> DeletePackAsync(long id);
    }
}
