using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace Allocations.Models.Results
{
    [SearchIndex(IndexerForType = typeof(ProductTestScenarioResult),
        CollectionName = "results",
        DatabaseName = "allocations",
        IndexerQuery = @"
            SELECT  tr.id, 
                    tr._ts,
                    tr.budget.id as budgetId,
                    tr.provider.id as providerId,
                    sr.fundingPolicy.id as fundingPolicyId,
                    sr.fundingPolicy.name as fundingPolicyName,
                    sr.scenarioName,
                    sr.testResult
            FROM tr
            JOIN sr IN tr.scenarioResults
            WHERE tr.documentType = 'ProviderTestResult'
            AND tr._ts > @HighWaterMark
        ")]
    public class ProductTestScenarioResultIndex
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

        [IsFacetable]
        [JsonProperty("fundingPolicyId")]
        public string FundingPolicyId { get; set; }

        [IsSearchable]
        [IsFacetable]
        [JsonProperty("fundingPolicyName")]
        public string FundingPolicyName { get; set; }

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