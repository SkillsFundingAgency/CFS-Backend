using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.External
{
    /// <summary>
    /// Represents a funding stream
    /// </summary>
    [Serializable]
    public class FundingStream
    {
        public FundingStream()
        {
        }

        public FundingStream(string fundingStreamCode, string fundingStreamName,
            IEnumerable<AllocationLine> allocationLines)
        {
            FundingStreamCode = fundingStreamCode;
            FundingStreamName = fundingStreamName;
            AllocationLines = allocationLines;
        }

        /// <summary>
        /// The identifier for the funding stream
        /// </summary>
        public string FundingStreamCode { get; set; }

        /// <summary>
        /// The description of the funding stream
        /// </summary>
        public string FundingStreamName { get; set; }

        /// <summary>
        /// The allocation lines that relate to this funding stream
        /// </summary>
        public IEnumerable<AllocationLine> AllocationLines { get; set; }
    }
}