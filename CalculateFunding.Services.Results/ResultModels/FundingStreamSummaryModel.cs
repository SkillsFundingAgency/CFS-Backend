using System.Collections.Generic;

namespace CalculateFunding.Services.Results.ResultModels
{
    public class FundingStreamSummaryModel
    {
        public string Name { get; set; }

        public List<AllocationLineSummaryModel> AllocationLines { get; } = new List<AllocationLineSummaryModel>();
    }
}
