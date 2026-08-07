using FluentValidation;
using SaaSPlatform.Application.DTOS.Auth;
using SaaSPlatform.Application.DTOS.Projects;
using SaaSPlatform.Application.DTOS.Tenants;
using SaaSPlatform.Application.DTOS.Users;
using SaaSPlatform.Application.DTOS.Tasks;

namespace SaaSPlatform.API.Configurations
{
    public class RegisterTenantDtoValidator : AbstractValidator<RegisterTenantDto>
    {
        public RegisterTenantDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Organization name is required.");
            RuleFor(x => x.Domain).NotEmpty().WithMessage("Domain name is required.");
            RuleFor(x => x.AdminName).NotEmpty().WithMessage("Admin full name is required.");
            RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().WithMessage("A valid admin email address is required.");
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
            RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("Passwords must match.");
            RuleFor(x => x.Plan).NotEmpty().WithMessage("Subscription plan is required.");
        }
    }

    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email address is required.");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
        }
    }

    public class CreateProjectDtoValidator : AbstractValidator<CreateProjectDto>
    {
        public CreateProjectDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Project name is required.");
            RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant context is required.");
        }
    }

    public class CreateTaskDtoValidator : AbstractValidator<CreateTaskDto>
    {
        public CreateTaskDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Task name is required.");
            RuleFor(x => x.ProjectId).NotEmpty().WithMessage("Project context is required.");
            RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant context is required.");
        }
    }
}
