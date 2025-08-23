namespace RagnarokBotWeb.Application.Models
{
    public class FastSpringItem
    {
        public string Product { get; set; }
        public int Quantity { get; set; }
    }

    public class FastspringSessionResponse
    {
        public string Id { get; set; }
        public string Currency { get; set; }
        public long Expires { get; set; }
        public object Order { get; set; }
        public string Account { get; set; }
        public double Subtotal { get; set; }
        public List<FastSpringItem> Items { get; set; }
    }
}
