namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class PackItemDto
    {
        public long ItemId { get; set; }
        public string ItemName { get; set; }
        public long PackId { get; set; }
        public int Amount { get; set; } = 1;
        public int AmmoCount { get; set; }
        public DateTime? Deleted { get; set; }
    }
}
