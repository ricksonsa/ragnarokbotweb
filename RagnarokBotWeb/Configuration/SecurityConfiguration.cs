using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Enums;
using System.Security.Claims;
using System.Text;

namespace RagnarokBotWeb.Configuration
{
    public static class SecurityConfiguration
    {
        public static IServiceCollection AddAuthenticationModule(this IServiceCollection services)
        {
            var keyBytes = Encoding.UTF8.GetBytes("p2tfCQNn6FJrM7XmdAsW5zKc4DHyYbELwuPV93BRv8xeqkSjZa\r\nVhN64mSPatj9H5FqfU2rCTEWvpskKQy3eZwLGXnb8RudD7zBYM\r\nwRJXr2b6tsQZWNLUDV4C8nmpKyc7fagGqh5MFux39kASvPEdBz\r\nZd7wKDnsq8j9WTHaGmbAkeYN4RPJrEp3UXS5LCvQy6hzxBVMcF\r\nsDh3SGNHjf7qARkxzMe2VpyPncmbvJCKTX4ruZWtB86dLQ9YF5");
            var textKey = Convert.ToBase64String(keyBytes);
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
             .AddJwtBearer(options =>
             {
                 options.TokenValidationParameters = new TokenValidationParameters
                 {
                     ValidateIssuer = true,
                     ValidateAudience = true,
                     ValidateLifetime = true,
                     ValidateIssuerSigningKey = true,
                     ValidIssuer = "ragnarokbotowner",
                     ValidAudience = "ragnarokbotowner",
                     IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(textKey))
                 };

                 options.Events = new JwtBearerEvents
                 {
                     OnTokenValidated = context =>
                     {
                         var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;
                         var tokenType = claimsIdentity?.FindFirst(ClaimConstants.TokenType);
                         if (tokenType == null || tokenType.Value != ETokenType.AccessToken.ToString())
                             context.Fail("Unauthorized: Missing or invalid token type.");
                         return Task.CompletedTask;
                     }
                 };
             })
             .AddJwtBearer(AuthorizationPolicyConstants.IdTokenPolicy, options =>
             {
                 options.TokenValidationParameters = new TokenValidationParameters
                 {
                     ValidateIssuer = true,
                     ValidateAudience = true,
                     ValidateLifetime = true,
                     ValidateIssuerSigningKey = true,
                     ValidIssuer = "ragnarokbotowner",
                     ValidAudience = "ragnarokbotowner",
                     IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(textKey))
                 };

                 options.Events = new JwtBearerEvents
                 {
                     OnTokenValidated = context =>
                     {
                         var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;
                         var tokenType = claimsIdentity?.FindFirst(ClaimConstants.TokenType);
                         if (tokenType == null || tokenType.Value != ETokenType.IdToken.ToString())
                             context.Fail("Unauthorized: Missing or invalid token type.");
                         return Task.CompletedTask;
                     }
                 };
             });

            return services;
        }
    }
}
