using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Application.Models
{
    public class FileChangeCommand
    {
        public EFileChangeType FileChangeType { get; set; }
        public EFileChangeMethod FileChangeMethod { get; set; } = EFileChangeMethod.None;
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
