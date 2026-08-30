using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaaSPlatform.Application.DTOS.Reports
{
    public class QuarterlyStatDto
    {
        public int Year { get; set; }
        public int Quarter { get; set; }
        public int Count { get; set; }
    }
}