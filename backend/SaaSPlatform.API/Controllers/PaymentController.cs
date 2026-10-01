using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.API.Extensions;
using SaaSPlatform.Application.DTOS.Payments;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using SaaSPlatform_Utility;
using System;
using System.Threading.Tasks;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tenantId = User.GetTenantId();
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
            var tenantId = User.GetTenantId();
            if (tenantId == null) return Unauthorized();

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                SubscriptionPlanId = dto.SubscriptionPlanId,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod
            };

            var created = await _paymentService.CreateAsync(payment, User.GetUserId());
            return Ok(new ApiResponse<Payment>
            {
                Success = true,
                Message = "Payment created successfully",
                Data = created
            });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var tenantId = User.GetTenantId();
            if (tenantId == null) return Unauthorized();

            var payment = await _paymentService.GetByIdAsync(id);
            if (payment == null)
                return NotFound();

            // A payment can only be deleted inside its own tenant (super admins may delete any payment).
            if (payment.TenantId != tenantId.Value && !User.IsSuperAdmin())
                return NotFound();

            var result = await _paymentService.DeleteAsync(id, User.GetUserId());
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}
