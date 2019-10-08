using System.Collections.Generic;

namespace CalculateFunding.Models.Publishing
{
    /// <summary>
    /// Generated Provider Result - temporary class to use when refreshing to store generated data (from calculation results, template and core provider information)
    /// </summary>
    public class GeneratedProviderResult
    {
        /// <summary>
        /// Funding Lines
        /// </summary>
        public IEnumerable<FundingLine> FundingLines { get; set; }

        /// <summary>
        /// Calculations
        /// </summary>
        public IEnumerable<FundingCalculation> Calculations { get; set; }

        /// <summary>
        /// Reference data
        /// </summary>
        public IEnumerable<FundingReferenceData> ReferenceData { get; set; }

        /// <summary>
        /// Provider informations
        /// </summary>
        public Provider Provider { get; set; }

        /// <summary>
        /// Total Funding
        /// </summary>
        public decimal TotalFunding { get; set; }
    }
}
