using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Jobs
{
    public class JobQueryResponseModel
    {
        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("currentPage")]
        public int CurrentPage { get; set; }

        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }

        [JsonProperty("totalItems")]
        public int TotalItems { get; set; }

        [JsonProperty("results")]
        public IEnumerable<JobSummary> Results { get; set; }
    }
}
