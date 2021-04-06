using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Graph
{
    public class SpecificationNode : BaseNode
    {
        public override string PartitionKey => SpecificationId;

        [JsonProperty("specificationid")]
        public string SpecificationId { get; set; }
    }
}
