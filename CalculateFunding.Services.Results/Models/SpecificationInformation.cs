using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Results.Models
{
    public class SpecificationInformation
    {
        [JsonProperty("id")]
        public string Id { get; set; }
            
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("lastEditDate")]
        public DateTimeOffset? LastEditDate { get; set; }
        
        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }
        
        [JsonProperty("fundingStreamIds")]
        public IEnumerable<string> FundingStreamIds { get; set; }
            
        [JsonProperty("fundingPeriodEnd")]
        public DateTimeOffset? FundingPeriodEnd { get; set; }

        [JsonIgnore()]
        public bool IsDirty { get; set; }

        public void MergeMutableInformation(SpecificationInformation specificationInformation)
        {
            if (LastEditDate == specificationInformation.LastEditDate
                && FundingPeriodEnd == specificationInformation.FundingPeriodEnd
                && (FundingStreamIds ?? Enumerable.Empty<string>()).SequenceEqual(specificationInformation.FundingStreamIds ?? Enumerable.Empty<string>())
                && Name == specificationInformation.Name)
            {
                return;
            }

            // set flag to IsDirty so we persist to cosmos
            IsDirty = true;

            LastEditDate = specificationInformation.LastEditDate;
            FundingPeriodEnd = specificationInformation.FundingPeriodEnd;
            FundingStreamIds = specificationInformation.FundingStreamIds?.ToArray();
            Name = specificationInformation.Name;
        }
    }
}