using System.Collections.Generic;

namespace CalculateFunding.Models.Policy.TemplateBuilder
{
    public class FindTemplateVersionQuery
    {
        public string FundingStreamId { get; set; }
        
        public string FundingPeriodId { get; set; }
        
        public List<TemplateStatus> Statuses { get; set; }
    }
}