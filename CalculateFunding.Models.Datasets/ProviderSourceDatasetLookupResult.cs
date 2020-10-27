using System.Collections.Generic;

namespace CalculateFunding.Models.Datasets
{
    public class ProviderSourceDatasetLookupResult
    {
        public Dictionary<string, Dictionary<string, ProviderSourceDataset>> ProviderSourceDatasets { get; set; }
    }
}
