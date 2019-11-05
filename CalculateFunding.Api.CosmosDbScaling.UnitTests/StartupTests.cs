using System;
using System.Collections.Generic;
using CalculateFunding.Api.CosmosDbScaling.Controllers;
using CalculateFunding.API.CosmosDbScaling;
using CalculateFunding.Services.CosmosDbScaling;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Api.CosmosDbScaling.UnitTests
{
    [TestClass]
    public class StartupTests : IoCUnitTestBase
    {
        [TestMethod]
        public void ConfigureServices_RegisterDependenciesCorrectly()
        {
            // Arrange
            IConfigurationRoot configuration = CreateTestConfiguration();
            Startup target = new Startup(configuration);

            // Act
            target.ConfigureServices(Services);

            // Assert
            ResolveType<CosmosDbScalingController>().Should().NotBeNull(nameof(CosmosDbScalingController));
        }

        [TestMethod]
        public void ConfigureServices_WhenMajorMinorVersioningIsEnabled_RegisterDependenciesCorrectly()
        {
            // Arrange
            IConfigurationRoot configuration = CreateTestConfiguration();
            Startup target = new Startup(configuration);

            // Act
            target.ConfigureServices(Services);

            // Assert
            IServiceProvider serviceProvider = target.ServiceProvider;

            CosmosDbScalingService service = (CosmosDbScalingService)serviceProvider.GetService(typeof(ICosmosDbScalingService));

            service
              .Should()
              .BeOfType<CosmosDbScalingService>();
        }

        protected override Dictionary<string, string> AddToConfiguration()
        {
            Dictionary<string, string> configData = new Dictionary<string, string>
            {
                { "SearchServiceName", "ss-t1te-cfs"},
                { "SearchServiceKey", "test" },
                { "CosmosDbSettings:CollectionName", "cosmosscalingconfig" },
                { "CosmosDbSettings:DatabaseName", "calculate-funding" },
                { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" },
                { "resultsClient:ApiEndpoint", "https://localhost:7005/api/" },
                { "resultsClient:ApiKey", "Local" },
                { "calcsClient:ApiEndpoint", "https://localhost:7002/api" },
                { "calcsClient:ApiKey", "Local" },
                { "cosmosDbScalingClient:ApiEndpoint", "https://localhost:7003/api" },
                { "cosmosDbScalingClient:ApiKey", "Local" },
                { "jobsClient:ApiEndpoint", "https://localhost:7010/api/" },
                { "jobsClient:ApiKey", "Local" },
            };

            return configData;
        }
    }
}
