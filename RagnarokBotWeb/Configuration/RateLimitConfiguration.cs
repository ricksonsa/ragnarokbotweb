using RagnarokBotWeb.Configuration.Data;
using RagnarokBotWeb.Middlewares;

namespace RagnarokBotWeb.Configuration
{
    public static class RateLimitConfiguration
    {
        public static IServiceCollection AddRateLimiting(this IServiceCollection services)
        {
            services.AddSingleton(new RateLimitOptions
            {
                MaxRequests = 100,
                TimeWindow = TimeSpan.FromMinutes(1)
            });
            return services;
        }

        public static IApplicationBuilder UseSimpleRateLimit(this IApplicationBuilder builder,
            int maxRequests = 100, int timeWindowMinutes = 1)
        {
            return builder.UseMiddleware<RateLimitingMiddleware>(maxRequests, timeWindowMinutes);
        }
    }
}
