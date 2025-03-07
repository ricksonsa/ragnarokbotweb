using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class ValidateClaimMatchAttribute : ActionFilterAttribute
{
    private readonly string _claimType;
    private readonly string _routeParameter;

    public ValidateClaimMatchAttribute(string claimType, string routeParameter)
    {
        _claimType = claimType;
        _routeParameter = routeParameter;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var claimValue = user.Claims.FirstOrDefault(c => c.Type == _claimType)?.Value;
        if (string.IsNullOrEmpty(claimValue))
        {
            context.Result = new ForbidResult();
            return;
        }

        if (!context.RouteData.Values.TryGetValue(_routeParameter, out var routeValue) || routeValue?.ToString() != claimValue)
        {
            context.Result = new BadRequestObjectResult(new { message = $"URL parameter '{_routeParameter}' does not match claim '{_claimType}'" });
            return;
        }

        base.OnActionExecuting(context);
    }
}
