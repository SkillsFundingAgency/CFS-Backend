using System;

namespace CalculateFunding.Services.Core.Options
{
    public class EngineSettings
    {
        /// <summary>
        /// Number of providers to process at once (calculate, save, send test messages) in the Calculation Engine
        /// </summary>
        public int ProviderBatchSize { get; set; } = 100;

        /// <summary>
        /// Number of providers to process at once (calculate, save, send test messages) in the Calculation Engine
        /// </summary>
        public int MaxPartitionSize { get; set; } = 100;

        /// <summary>
        /// Number of calculations to run in parallel
        /// </summary>
        public int CalculateProviderResultsDegreeOfParallelism { get; set; } = 5;

        public int SaveTestProviderResultsDegreeOfParallelism { get; set; } = 5;

        public int GetCurrentProviderTestResultsDegreeOfParallelism { get; set; } = 5;

        /// <summary>
        /// Number of parallel requests to lookup batches of calculation aggregations
        /// </summary>
        public int CalculationAggregationRetreivalParallelism { get; set; } = 15;

        [Obsolete("Remove once/if test engine is updated")]
        public int GetProviderSourceDatasetsDegreeOfParallelism { get; set; }
    }
}
