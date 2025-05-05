using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Exceptions;

namespace RagnarokBotWeb.Domain.Services
{
    public abstract class BaseService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BaseService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public long? TenantId(bool throwWhenNull = true)
        {
            var tenantIdString = GetClaimValue("http://schemas.microsoft.com/identity/claims/tenantid");
            try { return long.Parse(tenantIdString!); }
            catch (Exception)
            {
                if (throwWhenNull) throw new UnauthorizedException("Invalid server id");
                return null;
            }
        }

        public long? ServerId(bool throwWhenNull = true)
        {
            var serverIdString = GetClaimValue(ClaimConstants.ServerId);
            try { return long.Parse(serverIdString!); }
            catch (Exception)
            {
                if (throwWhenNull) throw new UnauthorizedException("Invalid server id");
                return null;
            }
        }

        public string? UserLogin()
        {
            var username = GetClaimValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            return username;
        }

        private string? GetClaimValue(string claimType)
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;
        }
    }
}
