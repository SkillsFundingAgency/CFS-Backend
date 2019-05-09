using Microsoft.Azure.Search;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CalculateFunding.Models.Results.Search
{
    public class ProviderCalculationResultsIndex
    {
        /// <summary>
        /// ID is the specificationId and ProviderId combined with an _
        /// </summary>
        [Key]
        [IsSearchable]
        [JsonProperty("id")]
        public string Id
        {
            get
            {
                return $"{SpecificationId}_{ProviderId}";
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

        [IsSearchable]
        [IsFilterable]
        [IsFacetable]
        [JsonProperty("calculationId")]
        public string[] CalculationId { get; set; }

        [IsSearchable]
        [IsFilterable]
        [IsFacetable]
        [JsonProperty("calculationName")]
        public string[] CalculationName { get; set; }

        [JsonProperty("calculationResult")]
        public string[] CalculationResult { get; set; }

        [JsonProperty("calculationException")]
        public string[] CalculationException { get; set; }

        [JsonProperty("calculationExceptionType")]
        public string[] CalculationExceptionType { get; set; }

        [JsonProperty("calculationExceptionMessage")]
        public string[] CalculationExceptionMessage { get; set; }
    }
}
