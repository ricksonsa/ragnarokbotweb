using RagnarokBotWeb.Domain.Exceptions;
using System.Net;

namespace RagnarokBotWeb.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {

            context.Response.ContentType = "application/json";
            if (exception is DomainException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            else if (exception is UnauthorizedException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
            else if (exception is NotFoundException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            var response = new
            {
                statusCode = context.Response.StatusCode,
                message = "An unexpected error occurred.",
                details = exception.Message
            };

            return context.Response.WriteAsJsonAsync(response);
        }
    }
}
