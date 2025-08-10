using System.Collections.Concurrent;

namespace RagnarokBotWeb.Middlewares
{
    // Rate limiting middleware
    public class RateLimitingMiddleware
    {
        private static readonly ConcurrentDictionary<string, List<DateTime>> _requests = new();
        private readonly RequestDelegate _next;
        private readonly int _maxRequests;
        private readonly TimeSpan _timeWindow;

        public RateLimitingMiddleware(RequestDelegate next, int maxRequests = 100, int timeWindowSeconds = 30)
        {
            _next = next;
            _maxRequests = maxRequests;
            _timeWindow = TimeSpan.FromSeconds(timeWindowSeconds);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientIP = GetClientIP(context);

            if (context.Request.Path.HasValue && context.Request.Path.Value.Contains("/api/bots"))
                await _next(context);

            if (IsRateLimited(clientIP))
            {
                context.Response.StatusCode = 429; // Too Many Requests
                if (context.Response.Headers.TryAdd("Retry-After", "60"))
                    await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");

                return;
            }

            await _next(context);
        }

        private bool IsRateLimited(string clientIP)
        {
            if (string.IsNullOrEmpty(clientIP)) return false;

            var now = DateTime.UtcNow;
            var cutoff = now - _timeWindow;

            // Get or create request history for this IP
            var requests = _requests.GetOrAdd(clientIP, new List<DateTime>());

            lock (requests)
            {
                // Remove old requests outside the time window
                requests.RemoveAll(r => r < cutoff);

                // Check if limit exceeded
                if (requests.Count >= _maxRequests)
                {
                    return true;
                }

                // Add current request
                requests.Add(now);
                return false;
            }
        }

        private string GetClientIP(HttpContext context)
        {
            // Check for forwarded IP first (if behind proxy/load balancer)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIP = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIP))
            {
                return realIP;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
