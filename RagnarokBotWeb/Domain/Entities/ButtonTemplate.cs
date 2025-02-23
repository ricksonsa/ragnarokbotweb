namespace RagnarokBotWeb.Domain.Entities
{
    public class ButtonTemplate : BaseEntity
    {
        public string Name { get; set; }
        public string Command { get; set; }
        public bool Public { get; set; } = true;
        public ChannelTemplate ChannelTemplate { get; set; }
    }
}
