using Microsoft.EntityFrameworkCore;
using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Domain.Entities;

[Index(nameof(ChannelType), IsUnique = true)]
public class ChannelTemplate : BaseEntity
{
    public string Name { get; set; }
    public string? CategoryName { get; set; }
    public EChannelType ChannelType { get; set; }
    public bool Admin { get; set; }
    public List<ButtonTemplate>? Buttons { get; set; }
}