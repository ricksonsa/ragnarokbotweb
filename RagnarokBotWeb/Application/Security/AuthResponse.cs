using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Application.Security
{
    public class AuthResponse
    {
        public string IdToken { get; set; }
        public string AccessToken { get; set; }
        public IEnumerable<ScumServer> ScumServers { get; set; }

        public AuthResponse() { }

        public AuthResponse(string accessToken)
        {
            AccessToken = accessToken;
        }
    }
}
