using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Models.Datasets.Schema;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets
{

    public class DefinitionSpecificationRelationship : Reference
    {
        public Reference DatasetDefinition { get; set; }

        public Reference Specification { get; set; }

        public string Description { get; set; }

        public DatasetRelationshipVersion DatasetVersion { get; set; }

        [JsonProperty("dataGranularity")]
        public DataGranularity DataGranularity { get; set; }

        [JsonProperty("definesScope")]
        public bool DefinesScope { get; set; }
    }


}
