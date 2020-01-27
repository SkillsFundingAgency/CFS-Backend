using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.DeadletterProcessor;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Functions.Datasets.UnitTests
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
                scope.ServiceProvider.GetService<IDefinitionsService>().Should().NotBeNull(nameof(IDefinitionsService));
                scope.ServiceProvider.GetService<IDatasetService>().Should().NotBeNull(nameof(IDatasetService));
                scope.ServiceProvider.GetService<IDatasetRepository>().Should().NotBeNull(nameof(IDatasetRepository));
                scope.ServiceProvider.GetService<IDatasetSearchService>().Should().NotBeNull(nameof(IDatasetSearchService));
                scope.ServiceProvider.GetService<IDefinitionSpecificationRelationshipService>().Should().NotBeNull(nameof(IDefinitionSpecificationRelationshipService));
                scope.ServiceProvider.GetService<ISpecificationsApiClient>().Should().NotBeNull(nameof(ISpecificationsApiClient));
                scope.ServiceProvider.GetService<IExcelDatasetReader>().Should().NotBeNull(nameof(IExcelDatasetReader));
                scope.ServiceProvider.GetService<ICalcsRepository>().Should().NotBeNull(nameof(ICalcsRepository));
                scope.ServiceProvider.GetService<IJobHelperService>().Should().NotBeNull(nameof(IJobHelperService));
            }
        }

        protected override Dictionary<string, string> AddToConfiguration()
        {
            var configData = new Dictionary<string, string>
            {
                { "SearchServiceName", "ss-t1te-cfs"},
                { "SearchServiceKey", "test" },
                { "CosmosDbSettings:DatabaseName", "calculate-funding" },
                { "CosmosDbSettings:ContainerName", "calcs" },
                { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" },
                { "specificationsClient:ApiEndpoint", "https://localhost:7001/api/" },
                { "specificationsClient:ApiKey", "Local" },
                { "resultsClient:ApiEndpoint", "https://localhost:7005/api/" },
                { "resultsClient:ApiKey", "Local" },
                { "calcsClient:ApiEndpoint", "https://localhost:7002/api/" },
                { "calcsClient:ApiKey", "Local" },
                { "jobsClient:ApiEndpoint", "https://localhost:7010/api/" },
                { "jobsClient:ApiKey", "Local" },
                { "providersClient:ApiEndpoint", "https://localhost:7011/api/" },
                { "providersClient:ApiKey", "Local" }
            };

            return configData;
        }
    }
}
