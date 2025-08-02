using Discord;

namespace RagnarokBotWeb.Application.Models
{
    public class CreateEmbed
    {
        public ulong DiscordId { get; set; }
        public ulong GuildId { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public string ImageUrl { get; set; }
        public string FooterText { get; set; }
        public Color Color { get; set; } = Color.Blue;

        public List<CreateEmbedButton> Buttons { get; set; }
    }

    public class CreateEmbedButton
    {
        public string Label { get; set; }
        public string ActionId { get; set; }

        public CreateEmbedButton(string label, string actionId)
        {
            Label = label;
            ActionId = actionId;
        }
    }
}
