using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.ProviderLegacy;

namespace CalculateFunding.Models.Calcs
{
    public class CalculationResultsModel
    {
        public ConcurrentBag<ProviderResult> ProviderResults { get; set; }

        public IEnumerable<ProviderSummary> PartitionedSummaries { get; set; }

        public bool ResultsContainExceptions
        {
            get
            {
                if (ProviderResults == null) return false;

                return ProviderResults.Any(m => m.CalculationResults != null && m.CalculationResults.Any(res => !string.IsNullOrWhiteSpace(res.ExceptionMessage)));
            }
        }

        public string ExceptionMessages
        {
            get
            {
                if (ProviderResults == null) return "";

                List<string> messages = new List<string>();
                foreach (ProviderResult providerResult in ProviderResults)
                {
                    foreach (CalculationResult calculationResult in providerResult.CalculationResults)
                    {
                        if (!string.IsNullOrWhiteSpace(calculationResult.ExceptionMessage))
                        {
                            messages.Add($"{calculationResult.Calculation?.Id ?? "" }: {calculationResult.ExceptionMessage}");
                        }
                    }
                }
                if (messages.Count > 0)
                {
                    return string.Join(Environment.NewLine, messages);
                }

                return string.Empty;
            }
        }

        public long CalculationRunMs { get; set; }
        public long AssemblyLoadMs { get; set; }
        public long ProviderSourceDatasetsLookupMs { get; set; }
    }
}
