using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class TestScenarioResult : IIdentifiable
    {
        /// <summary>
        /// ID is the TestScenario.Id and Provider.Id combined with an _
        /// </summary>
        [JsonProperty("id")]
        public string Id
        {
            get
            {
                return $"{TestScenario.Id}_{Provider.Id}";
            }
        }

        [JsonProperty("testResult")]
        public TestResult TestResult { get; set; }

        [JsonProperty("specification")]
        public Reference Specification { get; set; }

        [JsonProperty("testScenario")]
        public Reference TestScenario { get; set; }

        [JsonProperty("provider")]
        public Reference Provider { get; set; }

        public bool IsValid()
        {
            return TestResult != default(TestResult) &&
                Specification != null &&
                !string.IsNullOrWhiteSpace(Specification.Id) &&
                !string.IsNullOrWhiteSpace(Specification.Name) &&
                TestScenario != null &&
                !string.IsNullOrWhiteSpace(TestScenario.Id) &&
                !string.IsNullOrWhiteSpace(TestScenario.Name) &&
                Provider != null &&
                !string.IsNullOrWhiteSpace(Provider.Id) &&
                !string.IsNullOrWhiteSpace(Provider.Name) &&
                !string.IsNullOrWhiteSpace(Id);
        }
    }
}