using System.Collections.Generic;

namespace CalculateFunding.Models.Jobs
{
    public class JobQueryResponseModel
    {
        public int PageSize { get; set; }

        public int CurrentPage { get; set; }

        public int TotalPages { get; set; }

        public int TotalItems { get; set; }

        public IEnumerable<JobSummary> Results { get; set; }
    }
}
