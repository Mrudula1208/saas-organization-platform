using Microsoft.Identity.Client.Extensions.Msal;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SaaSPlatform.API.Middleware
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;

        }

        public async Task Invoke(HttpContext context)//1

            // Reads header from request
        {
            var tenantIdHeader = context.Request.Headers["TenantId"].ToString();//2
            if (!string.IsNullOrEmpty(tenantIdHeader))
            {
                //Converts string → Guid safely
                if (Guid.TryParse(tenantIdHeader, out Guid tenantId))//3
                {
                    //Stores TenantId in global request storage
                    context.Items["tenantId"] = tenantId;//4
                }
            }

                  //pass  request to the next step (controller)
            await _next(context);//5
        }

    }
}
//1.Client sends request with TenantId
//        ↓
//2. Middleware extracts TenantId
//        ↓
//3. Stores in HttpContext.Items
//        ↓
//4. Controller reads TenantId
//        ↓
//5. Service receives TenantId
//        ↓
//6. Repository filters using TenantId
//        ↓
//7.Database returns filtered data
//        ↓
//8. Response sent to client


//DATA RETURNS BACK
//Repository → Service → Controller → Client