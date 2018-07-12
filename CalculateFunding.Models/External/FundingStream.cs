using System.Collections.Generic;

namespace CalculateFunding.Models.External
{
    public class FundingStream
    {
        public FundingStream(string fundingStreamCode, string fundingStreamName,
            IEnumerable<AllocationLine> allocationLines)
        {
            FundingStreamCode = fundingStreamCode;
            FundingStreamName = fundingStreamName;
            AllocationLines = allocationLines;
        }

        public string FundingStreamCode { get; set; }

        public string FundingStreamName { get; set; }

        public IEnumerable<AllocationLine> AllocationLines { get; set; }
    }
}