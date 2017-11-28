using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class ProviderResultIndex
    {
        [Key]
        [IsSearchable]
        [JsonProperty("id")]
        public string Id { get; set; }

        [IsFacetable]
        [JsonProperty("budgetId")]
        public string BudgetId { get; set; }

        [IsSearchable]
        [IsFacetable]
        [JsonProperty("budgetName")]
        public string BudgetName { get; set; }

        [IsFacetable]
        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [IsSearchable]
        [JsonProperty("providerName")]
        public string ProviderName { get; set; }

        [IsSearchable]
        [JsonProperty("localAuthority")]
        public string LocalAuthority { get; set; }


        [IsSearchable]
        [IsFacetable]
        [JsonProperty("testresult")]
        public string TestResult { get; set; }

        //[IsSearchable]
        //[JsonProperty("productFolder")]
        //public string ProductFolder { get; set; }
        //[IsSearchable]
        //[JsonProperty("product")]
        //public string Product { get; set; }

        //[JsonProperty("lastFailedDate")]
        //[IsFilterable]
        //[IsFacetable]
        //public DateTime? LastFailedDate { get; set; }


        //[JsonProperty("lastPassedDate")]
        //[IsFilterable]
        //[IsFacetable]
        //public DateTime? LastPassedDate { get; set; }

        //[JsonIgnore]
        //public string FullName => $"{FirstName} {LastName}";


    }
}