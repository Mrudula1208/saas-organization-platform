using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.DTOS.Payments;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform_Utility;
using System;

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
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var payments = await _paymentService.GetAllAsync(tenantId.Value);
            return Ok(payments);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                SubscriptionPlanId = dto.SubscriptionPlanId,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod
            };

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

        private Guid? GetTenantId()
        {
            var tenantClaim = User.FindFirst("TenantId")?.Value;
            if (tenantClaim != null && Guid.TryParse(tenantClaim, out var tenantId) && tenantId != Guid.Empty)
                return tenantId;
            return null;
        }
    }
}
