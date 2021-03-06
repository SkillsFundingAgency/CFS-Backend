﻿using Newtonsoft.Json;

namespace CalculateFunding.Models.Graph
{
    public class Specification
    {
        public const string IdField = "specificationid";

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("specificationid")]
        public string SpecificationId { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
