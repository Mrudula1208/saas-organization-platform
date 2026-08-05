using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.DTOS.Users
{
    public class CreateUserDto
    {
        [Required (ErrorMessage ="Name is Required")]
        public string Name { get; set; }
        [Required(ErrorMessage ="Email is Required")]
        [EmailAddress(ErrorMessage ="Invalid email format")]
        public string Email { get; set; }
        [Required(ErrorMessage ="Password is required")]
        [MaxLength(6,ErrorMessage ="password must be at least 6  characters")]
        public string Password { get; set; }
        public Guid TenantId { get; set; }

       
    }
}
