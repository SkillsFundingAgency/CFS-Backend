using System;
using System.Text;

namespace CalculateFunding.Models.Scenarios
{
    public class ScenarioSearchResult
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SpecificationName { get; set; }
        public string PeriodName { get; set; }
        public string Status { get; set; }
        public DateTimeOffset? LastUpdatedDate { get; set; }
    }
}
