using Microsoft.Azure.Search;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.Results
{
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
        public double? CalculationResult { get; set; }

        [IsFilterable]
        [JsonProperty("isExcluded")]
        public bool IsExcluded { get; set; }
    }
}
