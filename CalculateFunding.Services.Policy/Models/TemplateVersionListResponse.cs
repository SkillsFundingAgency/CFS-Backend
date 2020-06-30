using System.Collections.Generic;

namespace CalculateFunding.Services.Policy.Models
{
    public class TemplateVersionListResponse
    {
        public IEnumerable<TemplateSummaryResponse> PageResults { get; set; }
        public int TotalCount { get; set; }
    }
}