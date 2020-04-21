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
        /// Number of parallel saves for Calculation results per provider
        /// </summary>
        public int SaveProviderDegreeOfParallelism { get; set; } = 5;

        /// <summary>
        /// Number of calculations to run in parallel
        /// </summary>
        public int CalculateProviderResultsDegreeOfParallelism { get; set; } = 5;

        public int SaveTestProviderResultsDegreeOfParallelism { get; set; } = 5;

        public int GetCurrentProviderTestResultsDegreeOfParallelism { get; set; } = 5;

        public int GetProviderSourceDatasetsDegreeOfParallelism { get; set; } = 5;

        /// <summary>
        /// Number of providers to index into search for calculation results in a single batch
        /// </summary>
        public int CalculationResultSearchIndexBatchSize { get; set; } = 100;
        
        /// <summary>
        /// Feature toggle to control queueing test engine run after calc batch completes
        /// </summary>
        public bool IsTestEngineEnabled { get; set; }
    }
}
