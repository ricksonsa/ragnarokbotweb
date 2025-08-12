using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Infrastructure.Repositories.Interfaces
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        Task<Page<Payment>> GetPageByServerId(Paginator paginator, long serverId);
        Task<Payment?> FindByOrderNumberAsync(string orderNumber);
        Task<Payment?> FindOnePending();
    }
}
