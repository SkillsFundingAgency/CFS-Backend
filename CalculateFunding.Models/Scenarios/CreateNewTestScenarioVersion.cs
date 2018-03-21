using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Scenarios
{
    public class CreateNewTestScenarioVersion
    {
        public string SpecificationId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Scenario { get; set; }
        public string Id { get; set; }
    }
}
