
using CalculateFunding.Models.ProviderLegacy;
using System.Collections.Generic;

namespace CalculateFunding.Models.Calcs.Messages
{
    public class GenerateAllocationsResultsMessage
    {
        public BuildProject BuildProject { get; set; }

        public IEnumerable<ProviderSummary> ProviderSummaries { get; set; }
    }
}
