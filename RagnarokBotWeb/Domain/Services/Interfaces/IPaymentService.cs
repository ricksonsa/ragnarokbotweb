using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Services.Dto;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<Page<PaymentDto>> GetPayments(Paginator paginator);
        Task<PaymentDto?> GetPayment(long id);
        Task<PaymentDto> AddPayment();
        Task<PaymentDto> ConfirmPayment(string token);

        Task<PaymentDto> CancelPayment(string token);
        Task<PaymentDto> ConfirmPayment();
        Task<PaymentDto> CancelPayment(long id);
    }
}
