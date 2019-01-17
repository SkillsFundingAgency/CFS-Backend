using System;
using System.Collections.ObjectModel;

namespace CalculateFunding.Api.External.V2.Models
{
    /// <summary>
    /// Represents a funding stream
    /// </summary>
    [Serializable]
    public class FundingStream
    {
        /// <summary>
        /// The id of the funding stream
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The name of the funding stream
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The short name of the funding stream,
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// The assigned period type of the funding stream
        /// </summary>
        public AllocationFundingStreamPeriodTypeModel PeriodType { get; set; }

        /// <summary>
        /// The list of associated allocation lines for the funding stream
        /// </summary>
        public Collection<AllocationLine> AllocationLines { get; set; }

        /// <summary>
        /// A flag to indicate whether financial envelopes are required
        /// </summary>
        public bool RequireFinancialEnvelopes { get; set; }
    }
}