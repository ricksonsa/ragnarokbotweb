using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Application.Security
{
    public interface ITokenIssuer
    {
        string GenerateIdToken(User user);
        string GenerateAccessToken(User user, long serverId);
    }
}
