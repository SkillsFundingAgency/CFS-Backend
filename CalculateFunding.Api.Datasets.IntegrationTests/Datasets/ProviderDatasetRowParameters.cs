using System.Collections.Generic;

namespace CalculateFunding.Api.Datasets.IntegrationTests.Datasets
{
    public class ProviderDatasetRowParameters
    {
        public string Ukprn { get; set; }
        
        public string Status { get; set; }
        
        public string Name { get; set; }
        
        public string ProviderType { get; set; }
        
        public string ProviderSubType { get; set; }
        
        public IEnumerable<string> Predecessors { get; set; }

        public IEnumerable<string> Successors { get; set; }
    }
}