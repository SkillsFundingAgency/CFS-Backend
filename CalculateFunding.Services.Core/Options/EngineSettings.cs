namespace CalculateFunding.Services.Core.Options
{
    public class EngineSettings
    {
        public int ProviderBatchSize { get; set; } = 100;

        public int SaveProviderDegreeOfParallelism { get; set; } = 5;

        public int CalculateProviderResultsDegreeOfParallelism { get; set; } = 5;

        public int SaveTestProviderResultsDegreeOfParallelism { get; set; } = 5;

        public int GetCurrentProviderTestResultsDegreeOfParallelism { get; set; } = 5;
    }
}
