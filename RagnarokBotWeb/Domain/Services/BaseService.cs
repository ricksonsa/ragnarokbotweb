using RagnarokBotWeb.Application.Security;
using System.IdentityModel.Tokens.Jwt;

namespace RagnarokBotWeb.Domain.Services
{
    public abstract class BaseService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BaseService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public long? TenantId()
        {
            var tenantIdString = GetClaimValue(ClaimConstants.TenantId);
            try { return long.Parse(tenantIdString!); }
            catch (Exception) { return null; }
        }

        public long? ServerId()
        {
            var serverIdString = GetClaimValue(ClaimConstants.ServerId);
            try { return long.Parse(serverIdString!); }
            catch (Exception) { return null; }
        }

        public string? UserName()
        {
            var username = GetClaimValue(JwtRegisteredClaimNames.Sub);
            return username;
        }

        private string? GetClaimValue(string claimType)
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;
        }
    }
}
