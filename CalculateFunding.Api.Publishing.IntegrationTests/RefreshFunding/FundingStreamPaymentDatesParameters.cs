using CalculateFunding.Common.ApiClient.Publishing.Models;
using System.Collections.Generic;

namespace CalculateFunding.Api.Publishing.IntegrationTests.RefreshFunding
{
    public class FundingStreamPaymentDatesParameters
    {
        public string Id => $"{FundingStreamId}-{FundingPeriodId}";
        public string FundingPeriodId { get; set; }
        public string FundingStreamId { get; set; }
        public ICollection<FundingStreamPaymentDate> PaymentDates { get; set; }
    }
}
