using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class Specification : Reference
    {
        public Specification()
        {
            Policies = Enumerable.Empty<Policy>();
        }

        [JsonProperty("academicYear")]
        public Reference AcademicYear { get; set; }

        [JsonProperty("fundingStream")]
        public Reference FundingStream { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("policies")]
        public IEnumerable<Policy> Policies { get; set; }

        [JsonProperty("datasetDefinitions")]
        public List<DatasetDefinition> DatasetDefinitions { get; set; }

    }
}