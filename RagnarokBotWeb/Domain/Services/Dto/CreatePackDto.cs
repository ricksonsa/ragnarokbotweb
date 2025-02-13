namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class CreatePackDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public decimal VipPrice { get; set; }
        public List<ItemToPackDto> Items { get; set; }
    }

    public class ItemToPackDto()
    {
        public long ItemId { get; set; }
        public string? ItemName { get; set; }
        public string? ItemCode { get; set; }
        public int Amount { get; set; }
    }
}
