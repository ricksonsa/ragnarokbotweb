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
        public string GenerateIdToken(User user)
        {
            // TODO: Guardar chave em config
            var keyBytes = Encoding.UTF8.GetBytes("p2tfCQNn6FJrM7XmdAsW5zKc4DHyYbELwuPV93BRv8xeqkSjZa\r\nVhN64mSPatj9H5FqfU2rCTEWvpskKQy3eZwLGXnb8RudD7zBYM\r\nwRJXr2b6tsQZWNLUDV4C8nmpKyc7fagGqh5MFux39kASvPEdBz\r\nZd7wKDnsq8j9WTHaGmbAkeYN4RPJrEp3UXS5LCvQy6hzxBVMcF\r\nsDh3SGNHjf7qARkxzMe2VpyPncmbvJCKTX4ruZWtB86dLQ9YF5");
            var textKey = Convert.ToBase64String(keyBytes);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(textKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(ClaimConstants.TenantId, user.Tenant?.Id.ToString() ?? string.Empty),
                new Claim(ClaimConstants.TokenType, ETokenType.IdToken.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "ragnarokbotowner",
                audience: "ragnarokbotowner",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateAccessToken(User user, long serverId)
        {
            // TODO: Guardar chave em config
            var keyBytes = Encoding.UTF8.GetBytes("p2tfCQNn6FJrM7XmdAsW5zKc4DHyYbELwuPV93BRv8xeqkSjZa\r\nVhN64mSPatj9H5FqfU2rCTEWvpskKQy3eZwLGXnb8RudD7zBYM\r\nwRJXr2b6tsQZWNLUDV4C8nmpKyc7fagGqh5MFux39kASvPEdBz\r\nZd7wKDnsq8j9WTHaGmbAkeYN4RPJrEp3UXS5LCvQy6hzxBVMcF\r\nsDh3SGNHjf7qARkxzMe2VpyPncmbvJCKTX4ruZWtB86dLQ9YF5");
            var textKey = Convert.ToBase64String(keyBytes);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(textKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(ClaimConstants.TenantId, user.Tenant?.Id.ToString() ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimConstants.TokenType, ETokenType.AccessToken.ToString()),
                new Claim(ClaimConstants.ServerId, serverId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "ragnarokbotowner",
                audience: "ragnarokbotowner",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
