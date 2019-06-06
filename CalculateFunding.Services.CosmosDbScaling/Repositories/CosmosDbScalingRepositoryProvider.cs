using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using System;
using Microsoft.Extensions.DependencyInjection;
using CalculateFunding.Models.CosmosDbScaling;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public class CosmosDbScalingRepositoryProvider : ICosmosDbScalingRepositoryProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public CosmosDbScalingRepositoryProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ICosmosDbScalingRepository GetRepository(CosmosRepositoryType repositoryType)
        {
            switch (repositoryType)
            {
                case CosmosRepositoryType.CalculationProviderResults:
                    return _serviceProvider.GetService<CalculationProviderResultsScalingRepository>();
                case CosmosRepositoryType.ProviderSourceDatasets:
                    return _serviceProvider.GetService<ProviderSourceDatasetsScalingRepository>();
                case CosmosRepositoryType.PublishedProviderResults:
                    return _serviceProvider.GetService<PublishedProviderResultsScalingRepository>();
                default:
                    throw new ArgumentException("Invalid repository type provided");
            }
        }
    }
}
