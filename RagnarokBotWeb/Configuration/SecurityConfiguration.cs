using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RagnarokBotWeb.Configuration
{
    public static class SecurityConfiguration
    {
        public static IServiceCollection AddAuthenticationModule(this IServiceCollection services)
        {
            var keyBytes = Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("jwt_secret")!);
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
             .AddJwtBearer(options =>
             {
                 options.TokenValidationParameters = new TokenValidationParameters
                 {
                     ValidateIssuer = true,
                     ValidateAudience = true,
                     ValidateLifetime = true,
                     ValidateIssuerSigningKey = true,
                     ValidIssuer = "ragnarokbot.com",
                     ValidAudience = "ragnarokbot.com",
                     NameClaimType = JwtRegisteredClaimNames.Sub,
                     IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
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
                     ValidIssuer = "ragnarokbot.com",
                     NameClaimType = JwtRegisteredClaimNames.Sub,
                     ValidAudience = "ragnarokbot.com",
                     IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
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
