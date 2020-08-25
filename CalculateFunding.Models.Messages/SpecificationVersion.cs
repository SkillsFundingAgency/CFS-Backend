using System;
using System.Collections.Generic;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Messages
{
    public class SpecificationVersion
    {     
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("fundingPeriod")]
        public Reference FundingPeriod { get; set; }

        [JsonProperty("providerVersionId")]
        public string ProviderVersionId { get; set; }

        [JsonProperty("fundingStreams")]
        public IEnumerable<Reference> FundingStreams { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("dataDefinitionRelationshipIds")]
        public IEnumerable<string> DataDefinitionRelationshipIds { get; set; }

        [JsonProperty("templateIds")]
        public Dictionary<string, string> TemplateIds { get; set; } = new Dictionary<string, string>();

        [JsonProperty("externalPublicationDate")]
        public DateTimeOffset? ExternalPublicationDate { get; set; }

        [JsonProperty("earliestPaymentAvailableDate")]
        public DateTimeOffset? EarliestPaymentAvailableDate { get; set; }
    }
}
