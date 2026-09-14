using System;
using System.Collections.Generic;

namespace SaaSPlatform.Application.DTOS
{
    // One small result object returned by every paginated list endpoint:
    // the rows for the current page plus the total count Angular needs for page controls.
    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = new();

        public int TotalCount { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }
    }
}
