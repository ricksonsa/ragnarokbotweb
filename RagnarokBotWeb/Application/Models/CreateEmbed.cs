using Discord;

namespace RagnarokBotWeb.Application.Models
{
    public class CreateEmbed
    {
        public required ulong DiscordId { get; set; }
        public required ulong GuildId { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public string ImageUrl { get; set; }
        public string FooterText { get; set; }
        public bool TimeStamp { get; set; }
        public Color Color { get; set; } = Color.DarkOrange;

        public List<CreateEmbedButton> Buttons { get; set; }
        public List<CreateEmbedField> Fields { get; set; }

        public CreateEmbed()
        {
            Buttons = new List<CreateEmbedButton>();
            Fields = new List<CreateEmbedField>();
        }

        public void AddField(CreateEmbedField value)
        {
            Fields.Add(value);
        }
    }

    public class CreateEmbedField(string title, string message, bool inline = false)
    {
        public string Title { get; set; } = title;
        public string Message { get; set; } = message;
        public bool Inline { get; set; } = inline;
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
