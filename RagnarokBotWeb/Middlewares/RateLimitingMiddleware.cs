using System.Collections.Concurrent;

namespace RagnarokBotWeb.Middlewares
{
    public class RateLimitingMiddleware
    {
        private static readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _requests = new();
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

            // Only apply rate limiting to /api/bots
            if (context.Request.Path.HasValue && context.Request.Path.Value.Contains("/api/bots"))
            {
                if (IsRateLimited(clientIP))
                {
                    context.Response.StatusCode = 429; // Too Many Requests
                    context.Response.Headers["Retry-After"] = "60";
                    await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                    return; // Important: don't call _next()
                }
            }

            // Continue pipeline
            await _next(context);
        }

        private bool IsRateLimited(string clientIP)
        {
            if (string.IsNullOrEmpty(clientIP)) return false;

            var now = DateTime.UtcNow;
            var cutoff = now - _timeWindow;

            // Get or create queue for this IP
            var queue = _requests.GetOrAdd(clientIP, _ => new ConcurrentQueue<DateTime>());

            // Remove old timestamps
            while (queue.TryPeek(out var oldest) && oldest < cutoff)
            {
                queue.TryDequeue(out _);
            }

            // Check limit
            if (queue.Count >= _maxRequests)
            {
                return true;
            }

            // Add current request
            queue.Enqueue(now);
            return false;
        }

        private string GetClientIP(HttpContext context)
        {
            // Check for forwarded IP first (if behind proxy/load balancer)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
                return forwardedFor.Split(',')[0].Trim();

            var realIP = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIP))
                return realIP;

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
