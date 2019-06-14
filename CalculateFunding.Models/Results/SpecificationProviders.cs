using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Results
{
    public class SpecificationProviders
    {
        [JsonProperty("specificationId")]
        public string SpecificationId;

        [JsonProperty("providers")]
        public IEnumerable<string> Providers;
    }
}
