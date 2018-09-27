using Newtonsoft.Json;
using System;

namespace CalculateFunding.Repositories.Common.Search.Results
{
    public class CalculationProviderResultSearchResult
    {
        public string Id { get; set; }

        public double? CalculationResult { get; set; }

        public string SpecificationId { get; set; }

        public string SpecificationName { get; set; }

        public string CalculationId { get; set; }
        
        public string CalculationName { get; set; }

        public string CalculationSpecificationId { get; set; }

        public string CalculationSpecificationName { get; set; }

        public string CalculationStatus { get; set; }

        public string CalculationType { get; set; }

        public string ProviderId { get; set; }

        [JsonProperty("name")]
        public string ProviderName { get; set; }

        public DateTimeOffset LastUpdatedDate { get; set; }

        public string ProviderType { get; set; }

        public string ProviderSubType { get; set; }

        [JsonProperty("authority")]
        public string LocalAuthority { get; set; }

        public string UKPRN { get; set; }

        public string URN { get; set; }

        public string UPIN { get; set; }

        public string EstablishmentNumber { get; set; }

        public DateTimeOffset? OpenDate { get; set; }
    }
}
