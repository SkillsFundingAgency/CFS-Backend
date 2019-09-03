using System.Collections.Generic;

namespace CalculateFunding.Models.Publishing
{
    public class GeneratedProviderResult
    {
        public IEnumerable<FundingLine> FundingLines { get; set; }

        public IEnumerable<FundingCalculation> Calculations { get; set; }

        public IEnumerable<FundingReferenceData> ReferenceData { get; set; }
    }
}
