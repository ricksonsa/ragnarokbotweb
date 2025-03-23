using System.ComponentModel.DataAnnotations;

namespace RagnarokBotWeb.Domain.Entities;

public class Reader : BaseEntity
{
    [MaxLength(50)]
    public string FileName { get; set; }
    [MaxLength(255)]
    public string Value { get; set; }
    public ScumServer ScumServer { get; set; }
    public bool Processed { get; set; }

    public Reader(string fileName, string value, ScumServer scumServer)
    {
        FileName = fileName;
        Value = value;
        ScumServer = scumServer;
        CreateDate = DateTime.Now;
        Processed = false;
    }

    public Reader() { }
}