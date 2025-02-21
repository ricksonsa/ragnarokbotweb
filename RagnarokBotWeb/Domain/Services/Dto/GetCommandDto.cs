using Shared.Enums;

namespace RagnarokBotWeb.Domain.Services.Dto
{
    public class GetCommandDto
    {
        public Guid Uuid { get; set; }
        public string? Coordinates { get; set; }
        public string? Target { get; set; }
        public string? Value { get; set; }
        public bool Completed { get; set; } = false;
        public ECommandType Type { get; set; }
        public long? OrderId { get; set; }
    }
}
