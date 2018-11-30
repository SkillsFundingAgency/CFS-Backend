using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CalculateFunding.Functions.Calcs.UnitTests
{
    [TestClass]
    public class IoConfigTests : IoCUnitTestBase
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
                scope.ServiceProvider.GetService<ICalculationsRepository>().Should().NotBeNull(nameof(ICalculationsRepository));
                scope.ServiceProvider.GetService<ICalculationService>().Should().NotBeNull(nameof(ICalculationService));
                scope.ServiceProvider.GetService<ICalculationsSearchService>().Should().NotBeNull(nameof(ICalculationsSearchService));
                scope.ServiceProvider.GetService<IBuildProjectsRepository>().Should().NotBeNull(nameof(IBuildProjectsRepository));
                scope.ServiceProvider.GetService<IPreviewService>().Should().NotBeNull(nameof(IPreviewService));
                scope.ServiceProvider.GetService<ICompilerFactory>().Should().NotBeNull(nameof(ICompilerFactory));
                scope.ServiceProvider.GetService<ISourceFileGeneratorProvider>().Should().NotBeNull(nameof(ISourceFileGeneratorProvider));
                scope.ServiceProvider.GetService<IProviderResultsRepository>().Should().NotBeNull(nameof(IProviderResultsRepository));
                scope.ServiceProvider.GetService<ISpecificationRepository>().Should().NotBeNull(nameof(ISpecificationRepository));
                scope.ServiceProvider.GetService<IBuildProjectsService>().Should().NotBeNull(nameof(IBuildProjectsService));
            }
        }

        protected override Dictionary<string, string> AddToConfiguration()
        {
            var configData = new Dictionary<string, string>
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
                { "datasetsClient:ApiEndpoint", "https://localhost:7004/api/"},
                { "datasetsClient:ApiKey", "Local"},
                { "jobsClient:ApiEndpoint", "https://localhost:7010/api/"},
                { "jobsClient:ApiKey", "Local"}
            };

            return configData;
        }
    }
}
