namespace RagnarokBotWeb.Domain.Entities;

public class ReaderPointer : BaseEntity
{
    public required int LineNumber { get; set; }
    public required string FileName { get; set; }
    public required long FileSize { get; set; }
    public required DateTime LastUpdated { get; set; }
    public required ScumServer ScumServer { get; set; }
    public required DateTime FileDate { get; set; }
}