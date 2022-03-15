using System;

namespace CalculateFunding.Publishing.AcceptanceTests.Models
{
    internal class ExpectedFundingGroupVariationReason
    {
        public Guid FundingGroupVersionVariationReasonId { get; set; }

        public string VariationReason { get; set; }

        public Guid FundingGroupVersionId { get; set; }
    }
}
