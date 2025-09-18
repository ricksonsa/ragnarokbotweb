using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Enums;

namespace RagnarokBotWeb.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class ValidateAccessLevelAttribute(AccessLevel levelRequired) : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;

        if (!(user.Identity?.IsAuthenticated ?? false))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var claimValue = user.Claims.FirstOrDefault(c => c.Type == ClaimConstants.AccessLevel)?.Value;
        if (string.IsNullOrEmpty(claimValue))
        {
            context.Result = new ForbidResult();
            return;
        }

        if (!Enum.TryParse<AccessLevel>(claimValue, out var userLevel))
        {
            context.Result = new ForbidResult();
            return;
        }

        // ✅ Use bitwise check for [Flags] enums
        if (!userLevel.HasFlag(levelRequired))
        {
            context.Result = new ForbidResult();
            return;
        }

        base.OnActionExecuting(context);
    }
}