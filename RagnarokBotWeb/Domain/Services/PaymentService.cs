
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Exceptions;
using RagnarokBotWeb.Domain.Services.Dto;
using RagnarokBotWeb.Domain.Services.Interfaces;
using RagnarokBotWeb.Infrastructure.Repositories.Interfaces;

namespace RagnarokBotWeb.Domain.Services
{
    public class PaymentService : BaseService, IPaymentService
    {
        private readonly IMapper _mapper;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly PayPalService _payPalService;
        private readonly IUserRepository _userRepository;

        public PaymentService(
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IPaymentRepository paymentRepository,
            PayPalService payPalService,
            IUnitOfWork unitOfWork,
            IUserRepository userRepository) : base(httpContextAccessor)
        {
            _mapper = mapper;
            _paymentRepository = paymentRepository;
            _payPalService = payPalService;
            _unitOfWork = unitOfWork;
            _userRepository = userRepository;
        }

        public async Task<PaymentDto?> GetPayment(long id)
        {
            var payment = await _paymentRepository.FindByIdAsync(id);
            return _mapper.Map<PaymentDto>(payment);
        }

        public async Task<Page<PaymentDto>> GetPayments(Paginator paginator)
        {
            var serverId = ServerId();
            var page = await _paymentRepository.GetPageByServerId(paginator, serverId!.Value);
            var content = page.Content.Select(_mapper.Map<PaymentDto>);
            return new Page<PaymentDto>(content, page.TotalPages, page.TotalElements, paginator.PageNumber, paginator.PageSize);
        }

        public async Task<PaymentDto> AddPayment()
        {
            var tenantId = TenantId();

            var cutoffTime = DateTime.UtcNow.AddHours(-3);
            var payments = await _unitOfWork
                .AppDbContext
                .Payments
                .Include(payment => payment.Tenant)
                .Where(payment => payment.Tenant.Id == tenantId!.Value)
                .Where(payment => payment.CreateDate > cutoffTime || payment.ExpireAt > DateTime.UtcNow)
                .AsNoTracking()
                .ToListAsync();

            if (payments.Count != 0) throw new DomainException("User already have an active subscription or a payment pending.");

            var user = await _userRepository.FindOneWithTenantAsync(u => u.Email == UserLogin()!);

            try
            {
                var returnUrl = $"https://thescumbot.com/dashboard/payment-success";
                var cancelUrl = $"https://thescumbot.com/dashboard/payment-canceled";
                var order = await _payPalService.CreateOrderAsync(
                7.99m,
                user.Country == "Brazil" ? "BRL" : "USD",
                "The SCUM Bot",
                returnUrl,
                cancelUrl
                );

                // Encontrar link de aprovação
                var approveLink = order.links.Find(l => l.rel == "approve");

                var subscription = await _unitOfWork.AppDbContext.Subscriptions.FirstOrDefaultAsync();
                subscription ??= new Subscription { RollingDays = 30 };
                var payment = new Payment
                {
                    OrderNumber = order.id,
                    SubscriptionId = subscription.Id,
                    TenantId = user.Tenant.Id,
                    Url = approveLink.href,
                };

                await _paymentRepository.CreateOrUpdateAsync(payment);
                await _paymentRepository.SaveAsync();

                return _mapper.Map<PaymentDto>(payment);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<PaymentDto> ConfirmPayment(string token)
        {
            var payment = await _paymentRepository.FindByOrderNumberAsync(token);
            if (payment is null) throw new NotFoundException("Payment not found");

            payment.Status = Enums.EPaymentStatus.Confirmed;
            payment.ConfirmDate = DateTime.UtcNow;
            payment.ExpireAt = DateTime.UtcNow.AddDays(payment.Subscription.RollingDays);

            await _paymentRepository.CreateOrUpdateAsync(payment);
            await _paymentRepository.SaveAsync();
            return _mapper.Map<PaymentDto>(payment);
        }

        public async Task<PaymentDto> CancelPayment(string token)
        {
            var payment = await _paymentRepository.FindByOrderNumberAsync(token);
            if (payment is null) throw new NotFoundException("Payment not found");

            payment.Status = Enums.EPaymentStatus.Canceled;

            await _paymentRepository.CreateOrUpdateAsync(payment);
            await _paymentRepository.SaveAsync();
            return _mapper.Map<PaymentDto>(payment);
        }
    }
}
