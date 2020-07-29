using System.Collections.Generic;

namespace CalculateFunding.Models.Calcs
{
    public class CalculationResultContainer
    {
        public IEnumerable<CalculationResult> CalculationResults { get; set; }
        public IEnumerable<FundingLineResult> FundingLineResults { get; set; }
    }
}
