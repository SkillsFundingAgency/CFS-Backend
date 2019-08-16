using System.Collections.Generic;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Functions.Publishing.UnitTests
{
    [TestClass]
    public class StartupTests : IoCUnitTestBase
    {
        [TestMethod]
        public void ConfigureServices_RegisterDependenciesCorrectly()
        {
            // Arrange
            IConfigurationRoot configuration = CreateTestConfiguration();

            // Act
            using (IServiceScope scope = Startup.RegisterComponents(new ServiceCollection(), configuration).CreateScope())
            {
                // Assert
                scope.ServiceProvider.GetService<IPublishService>().Should().NotBeNull(nameof(IPublishService));
                scope.ServiceProvider.GetService<IApproveService>().Should().NotBeNull(nameof(IApproveService));
                scope.ServiceProvider.GetService<IRefreshService>().Should().NotBeNull(nameof(IRefreshService));
                scope.ServiceProvider.GetService<IPublishedResultService>().Should().NotBeNull(nameof(IPublishedResultService));
                scope.ServiceProvider.GetService<ICalculationResultsRepository>().Should().NotBeNull(nameof(ICalculationResultsRepository));
                scope.ServiceProvider.GetService<IRefreshPrerequisiteChecker>().Should().NotBeNull(nameof(IRefreshPrerequisiteChecker));
            }
        }

        protected override Dictionary<string, string> AddToConfiguration()
        {
            Dictionary<string, string> configData = new Dictionary<string, string>
            {
                { "CosmosDbSettings:DatabaseName", "calculate-funding" },
                { "CosmosDbSettings:CollectionName", "calcs" },
                { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" },
                { "profilingClient:BaseUrl", "https://localhost:5003" },
                { "profilingClient:ApiKey", "Test" },
            };

            return configData;
        }
    }
}
