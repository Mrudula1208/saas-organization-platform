using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform_Utility
{
    public static class RoleConstants
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string TenantAdmin = "TenantAdmin";
        public const string Manager = "Manager";
        public const string Member = "Member";

        public const string AdminRoles = "SuperAdmin,TenantAdmin";
        public const string ManagementRoles = "SuperAdmin,TenantAdmin,Manager";
    }
}
