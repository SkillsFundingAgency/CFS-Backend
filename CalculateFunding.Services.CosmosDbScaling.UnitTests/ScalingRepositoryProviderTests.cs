using System;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using CalculateFunding.Services.CosmosDbScaling.Repositories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.CosmosDbScaling
{
    [TestClass]
    public class ScalingRepositoryProviderTests
    {
        [TestMethod]
        [DataRow(CosmosCollectionType.CalculationProviderResults, nameof(CalculationProviderResultsScalingRepository))]
        [DataRow(CosmosCollectionType.ProviderSourceDatasets, nameof(ProviderSourceDatasetsScalingRepository))]
        [DataRow(CosmosCollectionType.PublishedFunding, nameof(PublishedFundingScalingRepository))]
        [DataRow(CosmosCollectionType.Calculations, nameof(CalculationsScalingRepository))]
        [DataRow(CosmosCollectionType.Jobs, nameof(JobsScalingRepository))]
        [DataRow(CosmosCollectionType.DatasetAggregations, nameof(DatasetAggregationsScalingRepository))]
        [DataRow(CosmosCollectionType.Datasets, nameof(DatasetsScalingRepository))]
        [DataRow(CosmosCollectionType.Profiling, nameof(ProfilingScalingRepository))]
        [DataRow(CosmosCollectionType.Specifications, nameof(SpecificationsScalingRepository))]
        [DataRow(CosmosCollectionType.TestResults, nameof(TestResultsScalingRepository))]
        [DataRow(CosmosCollectionType.Tests, nameof(TestsScalingRepository))]
        [DataRow(CosmosCollectionType.Users, nameof(UsersScalingRepository))]
        public void GetRepository_GivenRepositoryType_ReturnsInstanceOfCorrectRepository(CosmosCollectionType cosmosRepositoryType, string repositoryTypeName)
        {
            //Arrange
            CosmosCollectionType repositoryType = cosmosRepositoryType;

            IServiceProvider serviceProvider = CreateServiceProvider();

            CosmosDbScalingRepositoryProvider scalingRepositoryProvider = new CosmosDbScalingRepositoryProvider(serviceProvider);

            //Act
            ICosmosDbScalingRepository scalingRepository = scalingRepositoryProvider.GetRepository(repositoryType);

            //Assert
            //AB: using gettype and name rather than assignable tp because ncrunch throws a wobbly
            scalingRepository
                .GetType()
                .Name
                .Should()
                .Be(repositoryTypeName);
        }

        [TestMethod]
        public void GetRepository_GivenRepositoryTypeIsNotValid_ThrowsArgumentException()
        {
            //Arrange
            CosmosCollectionType repositoryType = (CosmosCollectionType)42;

            IServiceProvider serviceProvider = CreateServiceProvider();

            CosmosDbScalingRepositoryProvider scalingRepositoryProvider = new CosmosDbScalingRepositoryProvider(serviceProvider);

            //Act
            Action test = () => scalingRepositoryProvider.GetRepository(repositoryType);

            //Assert
            test
                .Should()
                .ThrowExactly<ArgumentException>()
                .Which
                .Message
                .Should()
                .Be("Invalid repository type provided");
        }

        private static IServiceProvider CreateServiceProvider()
        {
            ICosmosRepository cosmosRepository = Substitute.For<ICosmosRepository>();

            IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();

            serviceProvider
                .GetService<CalculationProviderResultsScalingRepository>()
                .Returns(new CalculationProviderResultsScalingRepository(cosmosRepository));

            serviceProvider
                .GetService<ProviderSourceDatasetsScalingRepository>()
                .Returns(new ProviderSourceDatasetsScalingRepository(cosmosRepository));

            serviceProvider
                .GetService<PublishedFundingScalingRepository>()
                .Returns(new PublishedFundingScalingRepository(cosmosRepository));

            serviceProvider
                .GetService<CalculationsScalingRepository>()
                .Returns(new CalculationsScalingRepository(cosmosRepository));

            serviceProvider
                .GetService<JobsScalingRepository>()
                .Returns(new JobsScalingRepository(cosmosRepository));

            serviceProvider
                .GetService<DatasetAggregationsScalingRepository>()
                .Returns(new DatasetAggregationsScalingRepository(cosmosRepository));

            serviceProvider
                .GetService<DatasetsScalingRepository>()
                .Returns(new DatasetsScalingRepository(cosmosRepository));

            serviceProvider
                .GetService<ProfilingScalingRepository>()
                .Returns(new ProfilingScalingRepository(cosmosRepository));

            serviceProvider
                .GetService<SpecificationsScalingRepository>()
                .Returns(new SpecificationsScalingRepository(cosmosRepository));

            serviceProvider
                .GetService<TestResultsScalingRepository>()
                .Returns(new TestResultsScalingRepository(cosmosRepository));

            serviceProvider
                .GetService<TestsScalingRepository>()
                .Returns(new TestsScalingRepository(cosmosRepository));

            serviceProvider
                .GetService<UsersScalingRepository>()
                .Returns(new UsersScalingRepository(cosmosRepository));

            return serviceProvider;
        }
    }
}
