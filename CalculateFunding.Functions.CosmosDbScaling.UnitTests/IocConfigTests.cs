using System.Collections.Generic;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Functions.CosmosDbScaling.UnitTests
{
    [TestClass]
    public class IocConfigTests : IoCUnitTestBase
    {
        [TestMethod]
        public void ConfigureServices_RegisterDependenciesCorrectly()
        {
            // Arrange
            IConfigurationRoot configuration = CreateTestConfiguration();

            // Act
            using (var scope = IocConfig.Build(configuration).CreateScope())
            {
                // Assert
                scope.ServiceProvider.GetService<ICosmosDbScalingConfigRepository>().Should().NotBeNull(nameof(ICosmosDbScalingConfigRepository));
                scope.ServiceProvider.GetService<ICosmosDbScalingRepositoryProvider>().Should().NotBeNull(nameof(ICosmosDbScalingRepositoryProvider));
                scope.ServiceProvider.GetService<ICosmosDbScalingService>().Should().NotBeNull(nameof(ICosmosDbScalingService));
                scope.ServiceProvider.GetService<ICosmosDbScalingResiliencePolicies>().Should().NotBeNull(nameof(ICosmosDbScalingResiliencePolicies));
            }
        }

        protected override Dictionary<string, string> AddToConfiguration()
        {
            var configData = new Dictionary<string, string>
            {
                { "SearchServiceName", "ss-t1te-cfs"},
                { "SearchServiceKey", "test" },
                { "CosmosDbSettings:DatabaseName", "calculate-funding" },
                { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" },
                { "specificationsClient:ApiEndpoint", "https://localhost:7001/api/" },
                { "specificationsClient:ApiKey", "Local" },
                { "calcsClient:ApiEndpoint", "https://localhost:7002/api/" },
                { "calcsClient:ApiKey", "Local" },
                { "datasetsClient:ApiEndpoint", "https://localhost:7004/api/"},
                { "datasetsClient:ApiKey", "Local"},
                { "jobsClient:ApiEndpoint", "https://localhost:7010/api/"},
                { "jobsClient:ApiKey", "Local"},
                { "CommonStorageSettings:ConnectionString", "StorageConnection" }
            };

            return configData;
        }
    }
}
