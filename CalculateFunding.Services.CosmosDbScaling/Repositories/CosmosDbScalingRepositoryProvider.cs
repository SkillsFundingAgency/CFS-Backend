using System;
using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Services.CosmosDbScaling.Repositories
{
    public class CosmosDbScalingRepositoryProvider : ICosmosDbScalingRepositoryProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public CosmosDbScalingRepositoryProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ICosmosDbScalingRepository GetRepository(CosmosCollectionType repositoryType)
        {
            switch (repositoryType)
            {
                case CosmosCollectionType.CalculationProviderResults:
                    return _serviceProvider.GetService<CalculationProviderResultsScalingRepository>();

                case CosmosCollectionType.ProviderSourceDatasets:
                    return _serviceProvider.GetService<ProviderSourceDatasetsScalingRepository>();

                case CosmosCollectionType.PublishedFunding:
                    return _serviceProvider.GetService<PublishedFundingScalingRepository>();

                case CosmosCollectionType.Calculations:
                    return _serviceProvider.GetService<CalculationsScalingRepository>();

                case CosmosCollectionType.Jobs:
                    return _serviceProvider.GetService<JobsScalingRepository>();

                case CosmosCollectionType.DatasetAggregations:
                    return _serviceProvider.GetService<DatasetAggregationsScalingRepository>();

                case CosmosCollectionType.Datasets:
                    return _serviceProvider.GetService<DatasetsScalingRepository>();

                case CosmosCollectionType.Profiling:
                    return _serviceProvider.GetService<ProfilingScalingRepository>();

                case CosmosCollectionType.Specifications:
                    return _serviceProvider.GetService<SpecificationsScalingRepository>();

                case CosmosCollectionType.Users:
                    return _serviceProvider.GetService<UsersScalingRepository>();

                default:
                    throw new ArgumentException("Invalid repository type provided");
            }
        }
    }
}
