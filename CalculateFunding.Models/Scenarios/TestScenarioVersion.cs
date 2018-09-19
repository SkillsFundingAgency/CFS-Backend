using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalculateFunding.Models.Scenarios
{
    public class TestScenarioVersion : VersionedItem
    {
        //AB: These 2 properties are not required yet, will be updated during the story
        [JsonProperty("id")]
        public override string Id
        {
            get { return $"{TestScenarioId}_version_{Version}"; }
        }

        [JsonProperty("entityId")]
        public override string EntityId
        {
            get { return $"{TestScenarioId}"; }
        }

        [JsonProperty("testScenarioId")]
        public string TestScenarioId { get; set; }

        [JsonProperty("gherkin")]
        public string Gherkin { get; set; }

        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }

        [JsonProperty("fundingStreamIds")]
        public IEnumerable<string> FundingStreamIds { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        public override VersionedItem Clone()
        {
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<TestScenarioVersion>(json);
        }
    }
}