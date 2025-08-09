using Newtonsoft.Json;

namespace RagnarokBotWeb.Application.Models
{
    public class Link
    {
        [JsonProperty("href")]
        public string Href { get; set; }

        [JsonProperty("rel")]
        public string Rel { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }
    }

    public class PayPalLinkResponse
    {
        [JsonProperty("links")]
        public List<Link> Links { get; set; }
    }
}
