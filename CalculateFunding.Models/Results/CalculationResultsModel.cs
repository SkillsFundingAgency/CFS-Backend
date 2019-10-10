using System;
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
                if(ProviderResults == null) return false;

                return ProviderResults.Any(m => m.CalculationResults != null && m.CalculationResults.Any(res => !string.IsNullOrWhiteSpace(res.ExceptionMessage)));
            }
        }

        public string ExceptionMessages
        {
            get
            {
                if (ProviderResults == null) return "";

                var messages = ProviderResults.SelectMany(p => p.CalculationResults.Any(r => !string.IsNullOrWhiteSpace(r.ExceptionMessage))
                    ? p.CalculationResults
                        .Where(r => !string.IsNullOrWhiteSpace(r.ExceptionMessage))
                        .Select(c => $"{c.Calculation?.Id ?? "" }: {c.ExceptionMessage}")
                    : null);

                return string.Join(Environment.NewLine, messages);
            }
        }
    }
}
