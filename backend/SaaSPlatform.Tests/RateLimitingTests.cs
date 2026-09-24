using Microsoft.AspNetCore.RateLimiting;
using SaaSPlatform.API.Controllers;
using System.Reflection;
using Xunit;

namespace SaaSPlatform.Tests
{
    public class RateLimitingTests
    {
        [Fact]
        public void AuthController_HasEnableRateLimitingAttribute()
        {
            var attribute = typeof(AuthController).GetCustomAttribute<EnableRateLimitingAttribute>();

            Assert.NotNull(attribute);
            Assert.Equal("auth-policy", attribute!.PolicyName);
        }
    }
}
