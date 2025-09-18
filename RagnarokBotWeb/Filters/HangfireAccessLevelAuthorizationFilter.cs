using Hangfire.Dashboard;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Filters;

public class HangfireAccessLevelAuthorizationFilter(AccessLevel levelRequired) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;

        if (!(user.Identity?.IsAuthenticated ?? false))
        {
            return false;
        }

        var claimValue = user.Claims.FirstOrDefault(c => c.Type == ClaimConstants.AccessLevel)?.Value;
        if (string.IsNullOrEmpty(claimValue))
        {
            return false;
        }

        if (!Enum.TryParse<AccessLevel>(claimValue, true, out var userLevel))
        {
            return false;
        }

        return userLevel.HasFlag(levelRequired);
    }
}