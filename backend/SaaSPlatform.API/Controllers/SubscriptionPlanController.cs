using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Application.DTOS.SubscriptionPlans;
using SaaSPlatform.Application.Interfaces;
using SaaSPlatform.Domain.Entities;

namespace SaaSPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionPlanController : ControllerBase
    {
        private readonly ISubscriptionPlanService _subscriptionPlanService;

        public SubscriptionPlanController(ISubscriptionPlanService subscriptionPlanService)
        {
            _subscriptionPlanService = subscriptionPlanService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var plans = await _subscriptionPlanService.GetAllAsync();
            return Ok(plans);
        }

        [HttpGet("{Id}")]
        public async Task<IActionResult> GetById(Guid Id)
        {
            var plan = await _subscriptionPlanService.GetByIdAsync(Id);
            if (plan == null)
                return NotFound();

            return Ok(plan);
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSubscriptionPlanDto dto)
        {
            var plan = new SubscriptionPlan
            {
                Name = dto.Name,
                Price = dto.Price,
                MaxUsers = dto.MaxUsers,
                MaxProjects = dto.MaxProjects,
                StorageLimitMB = dto.StorageLimitMB,
                IsActive = true
            };

            var created = await _subscriptionPlanService.AddAsync(plan);
            return Ok(created);
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPut("{Id}")]
        public async Task<IActionResult> Update(Guid Id, [FromBody] UpdateSubscriptionPlanDto dto)
        {
            var plan = new SubscriptionPlan
            {
                Name = dto.Name,
                Price = dto.Price,
                MaxUsers = dto.MaxUsers,
                MaxProjects = dto.MaxProjects,
                StorageLimitMB = dto.StorageLimitMB,
                IsActive = dto.IsActive
            };

            var result = await _subscriptionPlanService.UpdateAsync(Id, plan);
            if (!result)
                return NotFound();

            return NoContent();
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpDelete("{Id}")]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var result = await _subscriptionPlanService.DeleteAsync(Id);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}
