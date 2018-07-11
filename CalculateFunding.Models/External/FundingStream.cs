using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.External
{
    public class FundingStream
    {
        public FundingStream(string fundingStreamCode, string fundingStreamName, IReadOnlyCollection<AllocationLine> allocationLines)
        {
            FundingStreamCode = fundingStreamCode;
            FundingStreamName = fundingStreamName;
            AllocationLines = allocationLines;
        }

        public string FundingStreamCode { get; set; }
        public string FundingStreamName { get; set;}
        public IReadOnlyCollection<AllocationLine> AllocationLines { get; }
    }
}
