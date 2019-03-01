using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Results
{
    public class CalculationResultsModel
    {
        public ConcurrentBag<ProviderResult> ProviderResults { get; set; }

        public IEnumerable<ProviderSummary> PartitionedSummaries { get; set; }

        public bool ResultsContainExceptions
        {
            get
            {
                if(ProviderResults == null)
                {
                    return false;
                }

                return ProviderResults.Any(m => m.CalculationResults != null && m.CalculationResults.Any(res => !string.IsNullOrWhiteSpace(res.ExceptionMessage)));
            }
        }
    }
}
