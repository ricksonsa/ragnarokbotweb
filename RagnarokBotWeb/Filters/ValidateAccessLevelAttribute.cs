using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RagnarokBotWeb.Application.Security;
using RagnarokBotWeb.Domain.Enums;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class ValidateAccessLevelAttribute : ActionFilterAttribute
{
    private readonly AccessLevel _levelRequired;

    public ValidateAccessLevelAttribute(AccessLevel levelRequired)
    {
        _levelRequired = levelRequired;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var claimValue = user.Claims.FirstOrDefault(c => c.Type == ClaimConstants.AccessLevel)?.Value;
        if (string.IsNullOrEmpty(claimValue) || claimValue != _levelRequired.ToString())
        {
            context.Result = new ForbidResult();
            return;
        }

        base.OnActionExecuting(context);
    }
}
