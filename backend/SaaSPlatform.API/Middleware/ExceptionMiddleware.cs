using System.Net;
using System.Security.Claims;
using System.Text.Json;
using SaaSPlatform.API.Extensions;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform_Utility;

namespace SaaSPlatform.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _environment;

        public ExceptionMiddleware(RequestDelegate next, IWebHostEnvironment environment)
        {
            _next = next;
            _environment = environment;
        }

        public async Task Invoke(HttpContext context, ISystemLogRepository systemLogs)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // Client aborted request or connection closed; abort cleanly without response writing
            }
            catch (Exception ex)
            {
                if (!context.Response.HasStarted)
                {
                    await LogErrorAsync(context, systemLogs, ex);
                    await HandleExceptionAsync(context, ex);
                }
            }
        }

        private static async Task LogErrorAsync(HttpContext context, ISystemLogRepository systemLogs, Exception ex)
        {
            try
            {
                var userId = context.User.GetUserId();
                var tenantId = context.User.GetTenantId();
                var path = context.Request.Path.Value ?? "/";
                var method = context.Request.Method;

                var (eventType, logMessage) = ex switch
                {
                    ArgumentException argEx => ("VALIDATION_FAILED", $"Validation error on {method} {path}: {argEx.Message}"),
                    UnauthorizedAccessException => ("SECURITY_ALERT", $"Unauthorized access attempted on {method} {path}."),
                    KeyNotFoundException notFoundEx => ("NOT_FOUND", $"Resource not found on {method} {path}: {notFoundEx.Message}"),
                    _ => ("SYSTEM_ERROR", $"Server error occurred processing {method} {path} ({ex.GetType().Name}).")
                };

                await systemLogs.LogAsync(eventType, logMessage, userId, tenantId);
            }
            catch
            {
                // Logging must never break the error response.
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            if (context.Response.HasStarted)
            {
                return Task.CompletedTask;
            }

            var (statusCode, message) = ex switch
            {
                ArgumentException argEx => (HttpStatusCode.BadRequest, argEx.Message),
                UnauthorizedAccessException => (HttpStatusCode.Forbidden, "You do not have permission to access this resource."),
                KeyNotFoundException notFoundEx => (HttpStatusCode.NotFound, notFoundEx.Message),
                _ => (HttpStatusCode.InternalServerError, _environment.IsDevelopment()
                    ? ex.InnerException?.Message ?? ex.Message
                    : "An unexpected error occurred. Please try again later.")
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new ApiResponse<object?>
            {
                Success = false,
                Message = message,
                Data = null
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
        }
    }
}
