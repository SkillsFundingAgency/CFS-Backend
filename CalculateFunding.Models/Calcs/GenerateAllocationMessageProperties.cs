using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Calcs
{
    public class GenerateAllocationMessageProperties
    {
        public string JobId { get; set; }

        public string SpecificationId { get; set; }

        public string ProviderCacheKey { get; set; }

        public string CalculationsAggregationsBatchCacheKey { get; set; }
       
        public int PartitionIndex { get; set; }

        public int PartitionSize{ get; set; }

        public int BatchCount { get; set; }

        public int BatchNumber { get; set; }

        public IEnumerable<string> CalculationsToAggregate { get; set; }

        public bool GenerateCalculationAggregationsOnly { get; set; }
    }
}
