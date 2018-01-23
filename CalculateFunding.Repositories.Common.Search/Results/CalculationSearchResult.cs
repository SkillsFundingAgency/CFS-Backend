using System;
using System.Text;

namespace CalculateFunding.Repositories.Common.Search.Results
{

    public class CalculationSearchResult
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string PeriodName { get; set; }
        public string SpecificationName { get; set; }
        public string Status { get; set; }
    }
}
