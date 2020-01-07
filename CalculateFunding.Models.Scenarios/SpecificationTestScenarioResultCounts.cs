using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Scenarios
{
    public class SpecificationTestScenarioResultCounts
    {
        public string SpecificationId { get; set; }

        public int Passed { get; set; }

        public int Failed { get; set; }

        public int Ignored { get; set; }

        public decimal TestCoverage
        {
            get
            {
                int totalRecords = Passed + Failed + Ignored;
                if (totalRecords == 0)
                {
                    return 0;
                }

                return Math.Round((decimal)(Passed + Failed) / totalRecords * 100, 1);
            }
        }
    }
}
