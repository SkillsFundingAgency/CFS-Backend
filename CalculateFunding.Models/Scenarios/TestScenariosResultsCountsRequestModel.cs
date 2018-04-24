using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Models.Scenarios
{
    public class TestScenariosResultsCountsRequestModel
    {
        public TestScenariosResultsCountsRequestModel()
        {
            TestScenarioIds = Enumerable.Empty<string>();
        }

        public IEnumerable<string> TestScenarioIds { get; set; }
    }
}
