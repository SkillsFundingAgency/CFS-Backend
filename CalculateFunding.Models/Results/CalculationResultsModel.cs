using CalculateFunding.Models.Results;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CalculateFunding.Models.Results
{
    public class CalculationResultsModel
    {
        public ConcurrentBag<ProviderResult> ProviderResults { get; set; }

        public IEnumerable<ProviderSummary> PartitionedSummaries { get; set; }
    }
}
