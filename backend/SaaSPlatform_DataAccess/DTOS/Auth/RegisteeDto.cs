using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.DTOS.Auth
{
    public class RegisteeDto
    {
        [Required]
        public string Email { get; set; }   
        [Required]
        public string Password { get; set; }
    }
}
