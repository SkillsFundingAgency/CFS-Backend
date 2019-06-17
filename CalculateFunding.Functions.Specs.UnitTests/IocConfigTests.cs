using System.Collections.Generic;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Functions.Specs.UnitTests
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
            using (IServiceScope scope = IocConfig.Build(configuration).CreateScope())
            {
                // Assert
                scope.ServiceProvider.GetService<IProviderVersionService>().Should().NotBeNull(nameof(IProviderVersionService));
                scope.ServiceProvider.GetService<ISpecificationsRepository>().Should().NotBeNull(nameof(ISpecificationsRepository));
                scope.ServiceProvider.GetService<ISpecificationsService>().Should().NotBeNull(nameof(ISpecificationsService));
                scope.ServiceProvider.GetService<ISpecificationsSearchService>().Should().NotBeNull(nameof(ISpecificationsSearchService));
                scope.ServiceProvider.GetService<IResultsRepository>().Should().NotBeNull(nameof(IResultsRepository));
            }
        }

        protected override Dictionary<string, string> AddToConfiguration()
        {
            Dictionary<string, string> configData = new Dictionary<string, string>
            {
                { "SearchServiceName", "ss-t1te-cfs"},
                { "SearchServiceKey", "test" },
                { "CosmosDbSettings:DatabaseName", "calculate-funding" },
                { "CosmosDbSettings:CollectionName", "calcs" },
                { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" },
                { "specificationsClient:ApiEndpoint", "https://localhost:7001/api/" },
                { "specificationsClient:ApiKey", "Local" },
                { "resultsClient:ApiEndpoint", "https://localhost:7005/api/" },
                { "resultsClient:ApiKey", "Local" },
                { "calcsClient:ApiEndpoint", "https://localhost:7002/api" },
                { "calcsClient:ApiKey", "Local" }
            };

            return configData;
        }
    }
}
