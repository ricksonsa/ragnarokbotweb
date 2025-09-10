using Hangfire.Dashboard;

namespace RagnarokBotWeb.Filters
{
    public class HangfireCustomBasicAuthenticationFilter : IDashboardAuthorizationFilter
    {
        public string User { get; set; } = string.Empty;
        public string Pass { get; set; } = string.Empty;

        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Check if Authorization header exists
            string? authHeader = httpContext.Request.Headers["Authorization"];
            if (authHeader != null && authHeader.StartsWith("Basic "))
            {
                var encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();
                var decodedUsernamePassword = System.Text.Encoding.UTF8.GetString(
                    Convert.FromBase64String(encodedUsernamePassword));

                var parts = decodedUsernamePassword.Split(':');
                if (parts.Length == 2)
                {
                    return parts[0] == User && parts[1] == Pass;
                }
            }

            // Return 401 if not authorized
            httpContext.Response.StatusCode = 401;
            httpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
            return false;
        }
    }
}
