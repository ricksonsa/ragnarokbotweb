using Microsoft.IdentityModel.Tokens;
using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RagnarokBotWeb.Application.Security
{
    public class TokenIssuer : ITokenIssuer
    {
        private readonly string _secretKey;

        public TokenIssuer()
        {
            _secretKey = Environment.GetEnvironmentVariable("jwt_secret")!;
        }

        public string GenerateIdToken(User user)
        {
            // TODO: Guardar chave em config
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(ClaimConstants.TenantId, user.Tenant?.Id.ToString() ?? string.Empty),
                new Claim(ClaimConstants.TokenType, ETokenType.IdToken.ToString()),
                new Claim(ClaimConstants.AccessLevel, user.AccessLevel.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "ragnarokbot.com",
                audience: "ragnarokbot.com",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateAccessToken(User user, long serverId)
        {
            // TODO: Guardar chave em config
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(ClaimConstants.TenantId, user.Tenant?.Id.ToString() ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimConstants.TokenType, ETokenType.AccessToken.ToString()),
                new Claim(ClaimConstants.AccessLevel, user.AccessLevel.ToString()),
                new Claim(ClaimConstants.ServerId, serverId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "ragnarokbot.com",
                audience: "ragnarokbot.com",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
