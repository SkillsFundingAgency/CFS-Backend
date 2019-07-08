using System.Collections.Generic;
using CalculateFunding.Services.Jobs.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Functions.Jobs.UnitTests
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
            using (IServiceScope scope = Startup.RegisterComponents(new ServiceCollection(), configuration).CreateScope())
            {
                // Assert
                scope.ServiceProvider.GetService<IJobRepository>().Should().NotBeNull(nameof(IJobRepository));
                scope.ServiceProvider.GetService<IJobDefinitionsRepository>().Should().NotBeNull(nameof(IJobDefinitionsRepository));
                scope.ServiceProvider.GetService<IJobManagementService>().Should().NotBeNull(nameof(IJobManagementService));
                scope.ServiceProvider.GetService<INotificationService>().Should().NotBeNull(nameof(INotificationService));
                scope.ServiceProvider.GetService<IJobDefinitionsService>().Should().NotBeNull(nameof(IJobDefinitionsService));
            }
        }

        protected override Dictionary<string, string> AddToConfiguration()
        {
            Dictionary<string, string> configData = new Dictionary<string, string>
            {
                { "CosmosDbSettings:DatabaseName", "calculate-funding" },
                { "CosmosDbSettings:CollectionName", "jobs" },
                { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" }
            };

            return configData;
        }
    }
}
