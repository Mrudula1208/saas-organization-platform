using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.DTOS.Billing;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BillingController : ControllerBase
    {
        private readonly IBillingService _billingService;

        public BillingController(IBillingService billingService)
        {
            _billingService = billingService;
        }

        [HttpGet("current-plan")]
        public async Task<IActionResult> GetCurrentPlan()
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var plan = await _billingService.GetCurrentPlanAsync(tenantId.Value);
            if (plan == null)
            {
                return NotFound(new { success = false, message = "No active subscription plan found for this tenant." });
            }
            return Ok(plan);
        }

        [HttpGet("payments")]
        public async Task<IActionResult> GetPayments()
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var payments = await _billingService.GetPaymentHistoryAsync(tenantId.Value);
            return Ok(payments);
        }

        [HttpGet("payments/{id}")]
        public async Task<IActionResult> GetPayment(Guid id)
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var payment = await _billingService.GetPaymentAsync(tenantId.Value, id);
            if (payment == null)
            {
                return NotFound(new { success = false, message = "Payment not found." });
            }
            return Ok(payment);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var tenantId = GetTenantId();
            if (tenantId == null) return Unauthorized();

            var summary = await _billingService.GetBillingSummaryAsync(tenantId.Value);
            return Ok(summary);
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