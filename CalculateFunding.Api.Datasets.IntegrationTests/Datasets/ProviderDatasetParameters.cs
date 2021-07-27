using System.Collections.Generic;

namespace CalculateFunding.Api.Datasets.IntegrationTests.Datasets
{
    public class ProviderDatasetParameters
    {
        public string Path { get; set; }
        
        public IEnumerable<ProviderDatasetRowParameters> Rows { get; set; }
    }
}