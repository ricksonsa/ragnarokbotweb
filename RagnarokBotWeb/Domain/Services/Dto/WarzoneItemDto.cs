namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class WarzoneItemDto
    {
        public long ItemId { get; set; }
        public string ItemName { get; set; }
        public long WarzoneId { get; set; }
        public int Priority { get; set; } = 1;
        public DateTime? Deleted { get; set; }
    }
}
