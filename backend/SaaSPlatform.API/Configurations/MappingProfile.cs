using AutoMapper;
using SaaSPlatform.Application.DTOS.Auth;
using SaaSPlatform.Application.DTOS.Projects;
using SaaSPlatform.Application.DTOS.Tenants;
using SaaSPlatform.Application.DTOS.Users;
using SaaSPlatform_Model;
using SaaSPlatform_Model.Entities;
using SaaSPlatform.Domain.Entities;

namespace SaaSPlatform.API.Configurations
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Tenant Mappings
            CreateMap<RegisterTenantDto, Tenant>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Domain, opt => opt.MapFrom(src => src.Domain))
                .ForMember(dest => dest.ContactEmail, opt => opt.MapFrom(src => src.AdminEmail))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<CreateTenantDto, Tenant>();
            CreateMap<UpdateTenantDto, Tenant>();

            // User Mappings
            CreateMap<CreateUserDto, User>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.Password))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<InviteUserDto, User>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => false)); // inactive until activated by link

            // Project Mappings
            CreateMap<CreateProjectDto, Project>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
            CreateMap<UpdateProjectDto, Project>();

            // Task Mappings
            CreateMap<CreateTaskDto, TaskItem>()
                .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
            CreateMap<UpdateTaskDto, TaskItem>();
        }
    }
}
