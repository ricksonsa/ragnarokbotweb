namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class PackDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public decimal VipPrice { get; set; }
        public List<ItemToPackDto> Items { get; set; }
    }
}
