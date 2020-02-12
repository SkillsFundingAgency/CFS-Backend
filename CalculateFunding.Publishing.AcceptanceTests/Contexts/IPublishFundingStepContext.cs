using CalculateFunding.Publishing.AcceptanceTests.Repositories;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface IPublishFundingStepContext
    {
        CalculationsInMemoryClient CalculationsInMemoryClient { get; }

        CalculationInMemoryRepository CalculationsInMemoryRepository { get; }
        
        ProfilingInMemoryClient ProfilingInMemoryClient { get; }

        void SetFeatureIsEnabled(string feature, bool flag);
    }
}
