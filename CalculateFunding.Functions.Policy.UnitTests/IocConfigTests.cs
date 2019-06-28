using System.Collections.Generic;
using CalculateFunding.Services.Policy;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Functions.Policy.UnitTests
{
    [TestClass]
    public class IocConfigTests : IoCUnitTestBase
    {
        [TestMethod]
        public void FunctionsNotifications_ConfigureServices_RegisterDependenciesCorrectly()
        {
            // Arrange
            IConfigurationRoot configuration = CreateTestConfiguration();

            // Act
            using (IServiceScope scope = IocConfig.Build(configuration).CreateScope())
            {
                // Assert
                scope.ServiceProvider.GetService<IPolicyRepository>().Should().NotBeNull(nameof(IPolicyRepository));
            }
        }

        protected override Dictionary<string, string> AddToConfiguration()
        {
            Dictionary<string, string> configData = new Dictionary<string, string>
            {
                { "CosmosDbSettings:DatabaseName", "calculate-funding" },
                { "CosmosDbSettings:CollectionName", "policy" },
                { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" }
            };

            return configData;
        }
    }
}
