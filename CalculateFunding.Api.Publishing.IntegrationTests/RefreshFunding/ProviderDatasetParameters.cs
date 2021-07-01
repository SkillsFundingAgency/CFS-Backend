using System.Collections.Generic;

namespace CalculateFunding.Api.Publishing.IntegrationTests.RefreshFunding
{
    public class ProviderDatasetParameters
    {
        public string Path { get; set; }
        
        public IEnumerable<ProviderDatasetRowParameters> Rows { get; set; }
    }
}