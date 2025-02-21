namespace Shared.Models
{
    public class ListPlayersCommandRequest
    {
        public string Content { get; set; }
        public ListPlayersCommandRequest(string content)
        {
            Content = content;
        }

    }
}
