using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Infrastructure.Configuration;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Infrastructure.Repositories
{
    public class PaymentRepository : Repository<Payment>, IPaymentRepository
    {
        private readonly AppDbContext _appDbContext;
        public PaymentRepository(AppDbContext context) : base(context)
        {
            _appDbContext = context;
        }

        public override Task<Payment?> FindByIdAsync(long id)
        {
            return DbSet()
                .Include(payment => payment.Subscription)
                .Include(payment => payment.Tenant)
                .Include(payment => payment.Tenant.ScumServers)
                .FirstOrDefaultAsync(payment => payment.Id == id);
        }

        public Task<Payment?> FindByOrderNumberAsync(string orderNumber)
        {
            return DbSet()
                .Include(payment => payment.Subscription)
                .Include(payment => payment.Tenant)
                .Include(payment => payment.Tenant.ScumServers)
                .FirstOrDefaultAsync(payment => payment.OrderNumber == orderNumber);
        }

        public Task<Payment?> FindOnePending()
        {
            return DbSet()
                .Include(payment => payment.Subscription)
                .Include(payment => payment.Tenant)
                .Include(payment => payment.Tenant.ScumServers)
                .FirstOrDefaultAsync(payment => payment.Status == Domain.Enums.EPaymentStatus.Waiting);
        }

        public Task<Page<Payment>> GetPageByServerId(Paginator paginator, long serverId)
        {
            var query = DbSet()
                .Include(payment => payment.Subscription)
                .Include(payment => payment.Tenant)
                .Include(payment => payment.Tenant.ScumServers)
                .Where(payment => payment.Tenant.ScumServers.Any(server => server.Id == serverId));

            return base.GetPageAsync(paginator, query);
        }

        public override Task CreateOrUpdateAsync(Payment entity)
        {
            return base.CreateOrUpdateAsync(entity);
        }
    }
}
