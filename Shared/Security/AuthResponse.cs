using Shared.Models;

namespace Shared.Security
{
    public class AuthResponse
    {
        public string IdToken { get; set; }
        public string AccessToken { get; set; }
        public List<ScumServer> ScumServers { get; set; }
    }
}
