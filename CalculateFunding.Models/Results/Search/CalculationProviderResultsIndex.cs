using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results.Search
{
    [SearchIndex(IndexerType = IndexerType.Search, IndexName = "calculationproviderresultsindex")]
    public class CalculationProviderResultsIndex
    {
        public CalculationProviderResultsIndex() { }

        // Have a separate constructor so deserialisation from Azure Search can convert null to false for IsExcluded property
        [JsonConstructor]
        public CalculationProviderResultsIndex(string specificationId,
            string specificationName,
            string calculationId,
            string calculationName,
            string calculationSpecificationId,
            string calculationType,
            string calculationSpecificationName,
            string providerId,
            string providerName,
            string providerType,
            string localAuthority,
            string providerSubType,
            DateTimeOffset lastUpdatedDate,
            string ukPrn,
            string urn,
            string upin,
            string establishmentNumber,
            DateTimeOffset? openDate,
            double? calculationResult,
            bool? isExcluded)
        {
            SpecificationId = specificationId;
            SpecificationName = specificationName;
            CalculationId = calculationId;
            CalculationName = calculationName;
            CalculationSpecificationId = calculationSpecificationId;
            CalculationType = calculationType;
            CalculationSpecificationName = calculationSpecificationName;
            ProviderId = providerId;
            ProviderName = providerName;
            ProviderType = providerType;
            LocalAuthority = localAuthority;
            ProviderSubType = providerSubType;
            LastUpdatedDate = lastUpdatedDate;
            UKPRN = ukPrn;
            URN = urn;
            UPIN = upin;
            EstablishmentNumber = establishmentNumber;
            OpenDate = openDate;
            CalculationResult = calculationResult;
            IsExcluded = isExcluded ?? false;
        }

        /// <summary>
        /// ID is the CalculationSpecificationId and ProviderId combined with an _
        /// </summary>
        [Key]
        [IsSearchable, IsRetrievable(true)]
        [JsonProperty("id")]
        public string Id
        {
            get
            {
                return $"{CalculationSpecificationId}_{ProviderId}";
            }
        }

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
        [JsonProperty("calculationId")]
        public string CalculationId { get; set; }

        [IsFilterable]
        [IsFacetable]
        [IsRetrievable(true)]
        [JsonProperty("calculationName")]
        public string CalculationName { get; set; }

        [IsFilterable]
        [IsFacetable]
        [IsRetrievable(true)]
        [JsonProperty("calculationSpecificationId")]
        public string CalculationSpecificationId { get; set; }

        [IsFilterable]
        [IsFacetable]
        [IsRetrievable(true)]
        [JsonProperty("calculationType")]
        public string CalculationType { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsSortable]
        [IsRetrievable(true)]
        [JsonProperty("calculationSpecificationName")]
        public string CalculationSpecificationName { get; set; }

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

        [JsonProperty("ukPrn")]
        [IsRetrievable(true)]
        public string UKPRN { get; set; }

        [JsonProperty("urn")]
        [IsRetrievable(true)]
        public string URN { get; set; }

        [JsonProperty("upin")]
        [IsRetrievable(true)]
        public string UPIN { get; set; }

        [JsonProperty("establishmentNumber")]
        [IsRetrievable(true)]
        public string EstablishmentNumber { get; set; }

        [JsonProperty("openDate")]
        [IsRetrievable(true)]
        public DateTimeOffset? OpenDate { get; set; }

        // NullValueHandling should be set to allow nulls when saving into search, otherwise the merge skips this property and null is never set
        [IsFilterable, IsSortable, IsRetrievable(true)]
        [JsonProperty("calculationResult", NullValueHandling = NullValueHandling.Include)]
        public double? CalculationResult { get; set; }

        [IsFilterable, IsRetrievable(true)]
        [JsonProperty("isExcluded")]
        public bool IsExcluded { get; set; }
    }
}
