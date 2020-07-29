using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    [SearchIndex(IndexerType = IndexerType.Search, IndexName = "providercalculationresultsindex")]
    public class ProviderCalculationResultsIndex
    {
        /// <summary>
        /// ID is the specificationId and ProviderId combined with an _
        /// </summary>
        [Key]
        [IsSearchable, IsRetrievable(true)]
        [JsonProperty("id")]
        public string Id => $"{SpecificationId}_{ProviderId}";

        [IsFilterable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsSortable]
        [IsRetrievable(true)]
        [JsonProperty("specificationName")]
        public string SpecificationName { get; set; }

        [IsFilterable]
        [IsFacetable]
        [IsRetrievable(true)]
        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsSortable]
        [IsRetrievable(true)]
        [JsonProperty("providerName")]
        public string ProviderName { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsFilterable]
        [IsSortable]
        [IsRetrievable(true)]
        [JsonProperty("providerType")]
        public string ProviderType { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsFilterable]
        [IsSortable]
        [IsRetrievable(true)]
        [JsonProperty("localAuthority")]
        public string LocalAuthority { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsFilterable]
        [IsSortable]
        [IsRetrievable(true)]
        [JsonProperty("providerSubType")]
        public string ProviderSubType { get; set; }

        [IsFilterable, IsSortable, IsRetrievable(true)]
        [JsonProperty("lastUpdatedDate")]
        public DateTimeOffset LastUpdatedDate { get; set; }

        [IsSearchable]
        [IsRetrievable(true)]
        [JsonProperty("ukPrn")]
        public string UKPRN { get; set; }

        [IsSearchable]
        [IsRetrievable(true)]
        [JsonProperty("urn")]
        public string URN { get; set; }

        [IsSearchable]
        [IsRetrievable(true)]
        [JsonProperty("upin")]
        public string UPIN { get; set; }

        [IsSearchable]
        [IsRetrievable(true)]
        [JsonProperty("establishmentNumber")]
        public string EstablishmentNumber { get; set; }

        [JsonProperty("openDate")]
        [IsRetrievable(true)]
        public DateTimeOffset? OpenDate { get; set; }

        [IsSearchable]
        [IsFilterable]
        [IsFacetable]
        [IsRetrievable(true)]
        [JsonProperty("calculationId")]
        public string[] CalculationId { get; set; }

        [IsSearchable]
        [IsFilterable]
        [IsFacetable]
        [IsRetrievable(true)]
        [JsonProperty("calculationName")]
        public string[] CalculationName { get; set; }

        [JsonProperty("calculationResult")]
        [IsRetrievable(true)]
        public string[] CalculationResult { get; set; }

        [IsFacetable]
        [IsSearchable]
        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("calculationException")]
        public string[] CalculationException { get; set; }

        [IsFacetable]
        [IsSearchable]
        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("calculationExceptionType")]
        public string[] CalculationExceptionType { get; set; }

        [IsFacetable]
        [IsSearchable]
        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("calculationExceptionMessage")]
        public string[] CalculationExceptionMessage { get; set; }

        [IsSearchable]
        [IsFilterable]
        [IsFacetable]
        [IsRetrievable(true)]
        [JsonProperty("fundingLineName")]
        public string[] FundingLineName { get; set; }

        [IsRetrievable(true)]
        [JsonProperty("fundingLineFundingStreamId")]
        public string[] FundingLineFundingStreamId { get; set; }

        [IsSearchable]
        [IsFilterable]
        [IsFacetable]
        [IsRetrievable(true)]
        [JsonProperty("fundingLineId")]
        public string[] FundingLineId { get; set; }

        [IsRetrievable(true)]
        [JsonProperty("fundingLineResult")]
        public string[] FundingLineResult { get; set; }

        [IsFacetable]
        [IsSearchable]
        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("fundingLineException")]
        public string[] FundingLineException { get; set; }

        [IsFacetable]
        [IsSearchable]
        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("fundingLineExceptionType")]
        public string[] FundingLineExceptionType { get; set; }

        [IsFacetable]
        [IsSearchable]
        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("fundingLineExceptionMessage")]
        public string[] FundingLineExceptionMessage { get; set; }
    }
}
