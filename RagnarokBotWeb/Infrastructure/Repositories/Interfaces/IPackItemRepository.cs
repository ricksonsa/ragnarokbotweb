using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IPackItemRepository : IRepository<PackItem>
    {
        void DeletePackItems(List<PackItem> packItems);
    }
}
