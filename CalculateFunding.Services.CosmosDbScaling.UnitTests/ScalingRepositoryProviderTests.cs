using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using CalculateFunding.Services.CosmosDbScaling.Repositories;
using CalculateFunding.Common.CosmosDb;
using FluentAssertions;
using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;

namespace CalculateFunding.Services.CosmosDbScaling
{
    [TestClass]
    public class ScalingRepositoryProviderTests
    {
        [TestMethod]
        public void GetRepository_GivenRepositoryTypeIsCalculationProviderResults_ReturnsInstanceOfCalculationProviderResultsScalingRepository()
        {
            //Arrange
            CosmosRepositoryType repositoryType = CosmosRepositoryType.CalculationProviderResults;

            IServiceProvider serviceProvider = CreateServiceProvider();

            CosmosDbScalingRepositoryProvider scalingRepositoryProvider = new CosmosDbScalingRepositoryProvider(serviceProvider);

            //Act
            ICosmosDbScalingRepository scalingRepository = scalingRepositoryProvider.GetRepository(repositoryType);

            //Assert
            scalingRepository
                .Should()
                .BeOfType<CalculationProviderResultsScalingRepository>();
        }

        [TestMethod]
        public void GetRepository_GivenRepositoryTypeIsProviderSourceDatasets_ReturnsInstanceOfProviderSourceDatasetsScalingRepository()
        {
            //Arrange
            CosmosRepositoryType repositoryType = CosmosRepositoryType.ProviderSourceDatasets;

            IServiceProvider serviceProvider = CreateServiceProvider();

            CosmosDbScalingRepositoryProvider scalingRepositoryProvider = new CosmosDbScalingRepositoryProvider(serviceProvider);

            //Act
            ICosmosDbScalingRepository scalingRepository = scalingRepositoryProvider.GetRepository(repositoryType);

            //Assert
            scalingRepository
                .Should()
                .BeOfType<ProviderSourceDatasetsScalingRepository>();
        }

        [TestMethod]
        public void GetRepository_GivenRepositoryTypeIsPublishedProviderResults_ReturnsInstanceOfPublishedProviderResultsScalingRepository()
        {
            //Arrange
            CosmosRepositoryType repositoryType = CosmosRepositoryType.PublishedProviderResults;

            IServiceProvider serviceProvider = CreateServiceProvider();

            CosmosDbScalingRepositoryProvider scalingRepositoryProvider = new CosmosDbScalingRepositoryProvider(serviceProvider);

            //Act
            ICosmosDbScalingRepository scalingRepository = scalingRepositoryProvider.GetRepository(repositoryType);

            //Assert
            scalingRepository
                .Should()
                .BeOfType<PublishedProviderResultsScalingRepository>();
        }

        [TestMethod]
        public void GetRepository_GivenRepositoryTypeIsNotValid_ThrowsArgumentException()
        {
            //Arrange
            CosmosRepositoryType repositoryType = (CosmosRepositoryType)42;

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
               .GetService<PublishedProviderResultsScalingRepository>()
               .Returns(new PublishedProviderResultsScalingRepository(cosmosRepository));

            return serviceProvider;
        }
    }
}
