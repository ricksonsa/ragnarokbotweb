using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<Order?> FindOneWithPackCreated();
    }
}
