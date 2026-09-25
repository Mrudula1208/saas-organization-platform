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

        [HttpGet("admin-transactions")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetAdminTransactions()
        {
            var transactions = await _paymentService.GetAdminTransactionsAsync();
            return Ok(transactions);
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

            var created = await _paymentService.CreateAsync(payment, GetUserId());
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
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var payment = await _paymentService.GetByIdAsync(id);
            if (payment == null)
                return NotFound();

            // A payment can only be deleted inside its own tenant (super admins may delete any payment).
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User.FindFirst("Role")?.Value;
            if (payment.TenantId != tenantId.Value && role != "SuperAdmin")
                return Forbid();

            var result = await _paymentService.DeleteAsync(id, GetUserId());
            if (!result)
                return NotFound();

            return NoContent();
        }

        private Guid? GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (claim != null && Guid.TryParse(claim, out var userId) && userId != Guid.Empty)
                return userId;
            return null;
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
