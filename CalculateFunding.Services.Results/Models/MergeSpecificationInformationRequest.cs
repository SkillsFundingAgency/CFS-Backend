using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Results.Models
{
    public class MergeSpecificationInformationRequest
    {
        [JsonProperty("specificationInformation")]
        public SpecificationInformation SpecificationInformation { get; set; }
        
        [JsonProperty("providerIds")]
        public IEnumerable<string> ProviderIds { get; set; }

        [JsonIgnore]
        public bool IsForAllProviders => (ProviderIds?.Any()).GetValueOrDefault() == false;
    }
}