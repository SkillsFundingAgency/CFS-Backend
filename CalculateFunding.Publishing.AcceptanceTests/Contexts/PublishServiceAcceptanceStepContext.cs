using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.FeatureManagement;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class PublishServiceAcceptanceStepContext : IPublishFundingStepContext
    {
        private readonly IFeatureManagerSnapshot _featureManagerSnapshot;

        public PublishServiceAcceptanceStepContext(IFeatureManagerSnapshot featureManagerSnapshot, 
            ICalculationsApiClient calculationsInMemoryClient, 
            ICalculationResultsRepository calculationsInMemoryRepository,
            IProfilingApiClient profilingApiClient)
        {
            _featureManagerSnapshot = featureManagerSnapshot;
            CalculationsInMemoryRepository = (CalculationInMemoryRepository)calculationsInMemoryRepository;
            CalculationsInMemoryClient = (CalculationsInMemoryClient)calculationsInMemoryClient;
            ProfilingInMemoryClient = (ProfilingInMemoryClient) profilingApiClient;
        }

        public void SetFeatureIsEnabled(string feature, bool flag)
        {
            ((InMemoryFeatureManagerSnapshot)_featureManagerSnapshot).SetIsEnabled(feature, flag);
        }

        public CalculationsInMemoryClient CalculationsInMemoryClient { get; }

        public CalculationInMemoryRepository CalculationsInMemoryRepository { get; }

        public ProfilingInMemoryClient ProfilingInMemoryClient { get; }
    }
}
