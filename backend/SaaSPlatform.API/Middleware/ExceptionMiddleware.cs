using System.Net;
using System.Security.Claims;
using System.Text.Json;
using SaaSPlatform.Application.Interfaces;

namespace SaaSPlatform.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext context, ISystemLogRepository systemLogs)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await LogErrorAsync(context, systemLogs, ex);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task LogErrorAsync(HttpContext context, ISystemLogRepository systemLogs, Exception ex)
        {
            try
            {
                var userId = ParseGuidClaim(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var tenantId = ParseGuidClaim(context.User.FindFirst("TenantId")?.Value);
                await systemLogs.LogAsync("SYSTEM_ERROR", $"Unexpected error: {ex.Message}", userId, tenantId);
            }
            catch
            {
                // Logging must never break the normal error response.
            }
        }

        private static Guid? ParseGuidClaim(string? value)
        {
            if (value != null && Guid.TryParse(value, out var id) && id != Guid.Empty)
                return id;
            return null;
        }
        private Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                success = false,
                message = ex.InnerException?.Message ?? ex.Message,
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));


        }
    }
}