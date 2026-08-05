using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform_Utility;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        public PaymentController(IPaymentService paymentService)
        {
            _paymentService= paymentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tenantId = Guid.Parse(User.FindFirst("TenantId")!.Value);
            // 👉 Get TenantId from JWT token

            var payments = await _paymentService.GetAllAsync(tenantId);
            return Ok(payments);
        }


        [HttpPost]
        public async Task<IActionResult>Create(Payment payment)
        {
            var tenantId = Guid.Parse(User.FindFirst("TenantId")!.Value);
            payment.TenantId = tenantId;

            var created = await _paymentService.CreateAsync(payment);
            return Ok(new ApiResponse<Payment>
            {
                Success = true,
                Message = "Payment created successfully",
                Data = created
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _paymentService.DeleteAsync(id);
            

            if (!result)
                return NotFound();
            

            return NoContent();
          
        }
    }
}
