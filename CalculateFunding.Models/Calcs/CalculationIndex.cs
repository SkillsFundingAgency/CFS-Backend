using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    [SearchIndex(IndexerForType = typeof(Calculation),
        CollectionName = "results",
        DatabaseName = "allocations",
        IndexerQuery = @"
            SELECT  tr.id, 
                    tr._ts,
                    tr.budget.id as budgetId,
                    tr.provider.id as providerId,
                    tr.budget.name as budgetName,
                    sr.fundingPolicy.id as fundingPolicyId,
                    sr.fundingPolicy.name as fundingPolicyName,
                    sr.productFolder.Id as productFolderId,
                    sr.productFolder.Name as productFolderName,
                    sr.allocationLine.Name as allocationLineName,
                    sr.scenarioName,
                    sr.testResult
            FROM tr
            JOIN sr IN tr.scenarioResults
            WHERE tr.documentType = 'ProviderTestResult'
            AND tr._ts > @HighWaterMark
        ")]
    public class CalculationIndex
    {
        [Key]
        [IsSearchable]
        [JsonProperty("id")]
        public string Id { get; set; }

        [IsSearchable]
        [JsonProperty("name")]
        public string Name { get; set; }


        [JsonProperty("calculationSpecification")]
        public Reference CalculationSpecification { get; set; }

        [JsonProperty("allocationLineName")]
        public string AllocationLineId { get; set; }
        [JsonProperty("allocationLineId")]
        public string AllocationLineName { get; set; }
        [JsonProperty("policySpecificationIds")]
        public List<string> PolicySpecificationIds { get; set; }
        [JsonProperty("policySpecificationNames")]
        public List<string> PolicySpecificationNames { get; set; }

        [IsSearchable]
        [JsonProperty("sourceCode")]
        public string SourceCode { get; set; }

    }
}