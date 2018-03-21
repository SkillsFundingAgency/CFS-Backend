using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Scenarios
{
    public class TestScenarioVersion : VersionedItem
    {
        [JsonProperty("gherkin")]
        public string Gherkin { get; set; }

        public override VersionedItem Clone()
        {
            return new TestScenarioVersion
            {
                PublishStatus = PublishStatus,
                Version = Version,
                Gherkin = Gherkin,
                Date = Date,
                Author = Author,
                Commment = Commment
            };
        }
    }
}