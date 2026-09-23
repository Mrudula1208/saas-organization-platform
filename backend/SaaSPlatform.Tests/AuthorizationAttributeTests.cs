using Microsoft.AspNetCore.Authorization;
using SaaSPlatform.API.Controllers;
using System.Reflection;
using Xunit;

namespace SaaSPlatform.Tests
{
    /// <summary>
    /// Verifies the [Authorize] attributes on the controllers.
    /// Unit tests bypass the security pipeline, so we check the attributes directly:
    /// if someone accidentally removes one, these tests fail.
    /// </summary>
    public class AuthorizationAttributeTests
    {
        [Fact]
        public void AuthEndpoints_MustStayPublic()
        {
            // Login and registration must work without a token.
            Assert.Null(typeof(AuthController).GetCustomAttribute<AuthorizeAttribute>());
        }

        [Theory]
        [InlineData(typeof(NotificationController))]
        [InlineData(typeof(TasksController))]
        [InlineData(typeof(BillingController))]
        [InlineData(typeof(PaymentController))]
        [InlineData(typeof(SystemLogController))]
        [InlineData(typeof(UserController))]
        [InlineData(typeof(ProjectController))]
        [InlineData(typeof(ProjectMembersController))]
        [InlineData(typeof(ReportsController))]
        [InlineData(typeof(TenantController))]
        public void Controllers_RequireAuthentication(Type controllerType)
        {
            var attribute = controllerType.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(attribute);
        }

        [Theory]
        [InlineData(typeof(ReportsController), "GetAdminDashboard")]
        [InlineData(typeof(ReportsController), "GetAdminReport")]
        [InlineData(typeof(ReportsController), "ExportAdminPdf")]
        [InlineData(typeof(ReportsController), "ExportAdminExcel")]
        [InlineData(typeof(SubscriptionPlanController), "Create")]
        [InlineData(typeof(SubscriptionPlanController), "Update")]
        [InlineData(typeof(SubscriptionPlanController), "Delete")]
        [InlineData(typeof(TenantController), "GetAll")]
        [InlineData(typeof(TenantController), "Create")]
        [InlineData(typeof(TenantController), "Delete")]
        public void SuperAdminOnlyEndpoints_RequireSuperAdminRole(Type controllerType, string methodName)
        {
            var method = controllerType.GetMethod(methodName);
            Assert.NotNull(method);

            var attribute = method!.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(attribute);
            Assert.Equal("SuperAdmin", attribute!.Roles);
        }

        [Theory]
        [InlineData(typeof(ProjectController), "Create")]
        [InlineData(typeof(ProjectController), "Update")]
        [InlineData(typeof(ProjectController), "Delete")]
        [InlineData(typeof(ProjectMembersController), "AddMember")]
        [InlineData(typeof(ProjectMembersController), "RemoveMember")]
        [InlineData(typeof(UserController), "Create")]
        [InlineData(typeof(UserController), "UpdateUser")]
        [InlineData(typeof(UserController), "DeleteUser")]
        [InlineData(typeof(UserController), "InviteUser")]
        [InlineData(typeof(TenantController), "Update")]
        [InlineData(typeof(TenantController), "UploadLogo")]
        public void AdminMutationEndpoints_RequireAdminRoles(Type controllerType, string methodName)
        {
            var method = controllerType.GetMethod(methodName);
            Assert.NotNull(method);

            var attribute = method!.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(attribute);
            Assert.Equal("SuperAdmin,TenantAdmin", attribute!.Roles);
        }

        [Fact]
        public void TenantSettingsUpdate_RequiresTenantAdminRole()
        {
            var method = typeof(TenantController).GetMethod("UpdateSettings");
            Assert.NotNull(method);

            var attribute = method!.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(attribute);
            Assert.Equal("TenantAdmin", attribute!.Roles);
        }

        [Fact]
        public void PasswordChange_HasNoUserIdParameter()
        {
            // Change-password works only on the authenticated user, so the
            // endpoint must not accept a target user id in the route or body.
            var method = typeof(UserController).GetMethod("ChangePassword");
            Assert.NotNull(method);

            var parameters = method!.GetParameters();
            Assert.Single(parameters);
            Assert.Equal("dto", parameters[0].Name);
        }
    }
}
