using System;
using Allocations.Repository;
using Newtonsoft.Json;

namespace Allocations.Models.Budgets
{

    public class Budget : DocumentEntity
    {
        public override string Id => $"{DocumentType}-{Name}".ToSlug();

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("academicYear")]
        public string AcademicYear { get; set; }

        [JsonProperty("fundingStream")]
        public string FundingStream { get; set; }

        [JsonProperty("fundingPolicies")]
        public FundingPolicy[] FundingPolicies { get; set; }

    }
}
