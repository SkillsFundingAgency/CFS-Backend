using System;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationReportIdentifier
    {
        public JobType JobType { get; set; }
        public string SpecificationId { get; set; }
        public string FundingStreamId { get; set; }
        public string FundingPeriodId { get; set; }
        public string FundingLineCode { get; set; }
        public string channelCode { get; set; }

        public override bool Equals(object obj)
        {
            return obj?.GetHashCode() == GetHashCode();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                JobType,
                SpecificationId,
                FundingStreamId,
                FundingPeriodId,
                FundingLineCode,
                channelCode);
        }
    }
}
