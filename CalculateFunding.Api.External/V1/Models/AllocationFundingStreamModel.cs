using CalculateFunding.Models.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CalculateFunding.Api.External.V1.Models
{

    /// <summary>
    /// Represents a funding stream
    /// </summary>
    [Serializable]
    public class AllocationFundingStreamModel
    {
        public AllocationFundingStreamModel()
        {
        }

        public AllocationFundingStreamModel(string fundingStreamCode, string fundingStreamName)
        {
            FundingStreamCode = fundingStreamCode;
            FundingStreamName = fundingStreamName;
        }

        /// <summary>
        /// The identifier for the funding stream
        /// </summary>
        public string FundingStreamCode { get; set; }

        /// <summary>
        /// The description of the funding stream
        /// </summary>
        public string FundingStreamName { get; set; }
    }
}
