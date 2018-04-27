using Microsoft.Azure.Search;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CalculateFunding.Models.Results
{
    public class CalculationProviderResultsIndex
    {
        /// <summary>
        /// ID is the CalculationSpecificationId and ProviderId combined with an _
        /// </summary>
        [Key]
        [IsSearchable]
        [JsonProperty("id")]
        public string Id
        {
            get
            {
                return $"{CalculationSpecificationId}_{ProviderId}";
            }
        }

        [IsFilterable, IsFacetable]
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsSortable]
        [JsonProperty("specificationName")]
        public string SpecificationName { get; set; }

        [IsFilterable]
        [IsFacetable]
        [JsonProperty("calculationId")]
        public string CalculationId { get; set; }

        [IsFilterable]
        [IsFacetable]
        [JsonProperty("calculationName")]
        public string CalculationName { get; set; }

        [IsFilterable]
        [IsFacetable]
        [JsonProperty("calculationSpecificationId")]
        public string CalculationSpecificationId { get; set; }

        [IsFilterable]
        [IsFacetable]
        [JsonProperty("calculationType")]
        public string CalculationType { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsSortable]
        [JsonProperty("calculationSpecificationName")]
        public string CalculationSpecificationName { get; set; }

        [IsFilterable]
        [IsFacetable]
        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsSortable]
        [JsonProperty("providerName")]
        public string ProviderName { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsFilterable]
        [IsSortable]
        [JsonProperty("providerType")]
        public string ProviderType { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsFilterable]
        [IsSortable]
        [JsonProperty("localAuthority")]
        public string LocalAuthority { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsFilterable]
        [IsSortable]
        [JsonProperty("providerSubType")]
        public string ProviderSubType { get; set; }

        [IsFilterable, IsSortable]
        [JsonProperty("lastUpdatedDate")]
        public DateTimeOffset LastUpdatedDate { get; set; }

        [JsonProperty("ukPrn")]
        public string UKPRN { get; set; }

        [JsonProperty("urn")]
        public string URN { get; set; }

        [JsonProperty("upin")]
        public string UPIN { get; set; }

        [JsonProperty("establishmentNumber")]
        public string EstablishmentNumber { get; set; }

        [JsonProperty("openDate")]
        public DateTimeOffset? OpenDate { get; set; }

        [IsFilterable, IsSortable]
        [JsonProperty("calculationResult")]
        public Double CaclulationResult { get; set; }
    }
}
