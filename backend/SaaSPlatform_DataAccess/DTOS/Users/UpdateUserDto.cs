using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.DTOS.Users
{
    public class UpdateUserDto
    {
        [Required]
        public string Name  { get; set; }

        [Required]
        public string Email { get; set; }
    }
}
