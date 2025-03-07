using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace RagnarokBotWeb.Application.Security
{
    public class AuthorizationPolicyConstants
    {
        public const string AccessTokenPolicy = JwtBearerDefaults.AuthenticationScheme;
        public const string IdTokenPolicy = "IdTokenJwtScheme";
    }
}
