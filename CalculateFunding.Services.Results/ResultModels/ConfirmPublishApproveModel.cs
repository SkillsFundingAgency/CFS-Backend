using System.Collections.Generic;

namespace CalculateFunding.Services.Results.ResultModels
{
    public class ConfirmPublishApproveModel
    {
        public int NumberOfProviders { get; set; }

        public string[] ProviderTypes { get; set; }

        public string[] LocalAuthorities { get; set; }

        public string FundingPeriod { get; set; }

        public List<FundingStreamSummaryModel> FundingStreams { get; } = new List<FundingStreamSummaryModel>();

        public decimal TotalFundingApproved { get; set; }
    }
}
