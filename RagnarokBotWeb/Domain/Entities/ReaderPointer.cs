using RagnarokBotWeb.Crosscutting.Utils;
using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Entities;

public class ReaderPointer : BaseEntity
{
    public required int LineNumber { get; set; }
    public required string FileName { get; set; }
    public required EFileType FileType { get; set; }
    public required ScumServer ScumServer { get; set; }
    public required DateTime FileDate { get; set; }
}