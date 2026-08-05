//using System.Net;
//using System.Text.Json;
//namespace SaaSPlatform.API.Middleware
//{
//    public class ErrorHandlingMiddleware
//    {
//        private readonly RequestDelegate _next;
//        public ErrorHandlingMiddleware(RequestDelegate next)
//        {
//            _next = next;
//        }

//        public async Task Invoke(HttpContext context)
//        {
//            try
//            {
//                await _next(context);
//            }
//            catch (Exception ex)
//            {
//                await HandleException(context, ex);
//            }
//        }

//        private static Task HandleException(HttpContext context, Exception ex)
//        {
//            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

//            var response = new
//            {
//                success = false,
//                message = ex.Message
//            };


//            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
//        }
//    }
//}