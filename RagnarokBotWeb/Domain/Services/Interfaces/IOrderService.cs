using RagnarokBotWeb.Application.Pagination;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Dto;

namespace RagnarokBotWeb.Domain.Services.Interfaces
{
    public interface IOrderService
    {
        Task<Order?> PlaceDeliveryOrder(string identifier, long packId);
        Task<Order?> PlaceDeliveryOrderFromDiscord(ulong guildId, ulong discordId, long packId);
        Task<Order?> PlaceWarzoneOrderFromDiscord(ulong guildId, ulong discordId, long warzoneId);
        Task<Order?> PlaceUavOrderFromDiscord(ScumServer server, ulong userDiscordId, string sector);
        Task<OrderDto?> PlaceWelcomePackOrder(long playerId);
        Task<Order?> PlaceWelcomePackOrder(Player player);
        Task<IEnumerable<Order>> GetCreatedOrders();
        Task<Page<OrderDto>> GetPacksPageByFilterAsync(Paginator paginator, string? filter);
        Task<OrderDto> ConfirmOrderDelivered(long orderId);
        Task<List<GrapthDto>> GetBestSellingOrdersPacks();
        Task ResetCommandOrders(long serverId);
    }
}
