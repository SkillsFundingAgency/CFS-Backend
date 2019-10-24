using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.TestRunner;
using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Functions.TestEngine.UnitTests
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
                scope.ServiceProvider.GetService<IBuildProjectRepository>().Should().NotBeNull(nameof(IBuildProjectRepository));
                scope.ServiceProvider.GetService<IGherkinParserService>().Should().NotBeNull(nameof(IGherkinParserService));
                scope.ServiceProvider.GetService<IGherkinParser>().Should().NotBeNull(nameof(IGherkinParser));
                scope.ServiceProvider.GetService<ICodeMetadataGeneratorService>().Should().NotBeNull(nameof(ICodeMetadataGeneratorService));
                scope.ServiceProvider.GetService<IStepParserFactory>().Should().NotBeNull(nameof(IStepParserFactory));
                scope.ServiceProvider.GetService<ITestResultsRepository>().Should().NotBeNull(nameof(ITestResultsRepository));
                scope.ServiceProvider.GetService<ISpecificationsApiClient>().Should().NotBeNull(nameof(ISpecificationsApiClient));
                scope.ServiceProvider.GetService<IScenariosRepository>().Should().NotBeNull(nameof(IScenariosRepository));
                scope.ServiceProvider.GetService<ITestEngineService>().Should().NotBeNull(nameof(ITestEngineService));
                scope.ServiceProvider.GetService<ITestEngine>().Should().NotBeNull(nameof(ITestEngine));
                scope.ServiceProvider.GetService<IGherkinExecutor>().Should().NotBeNull(nameof(IGherkinExecutor));
                scope.ServiceProvider.GetService<IProviderSourceDatasetsRepository>().Should().NotBeNull(nameof(IProviderSourceDatasetsRepository));
                scope.ServiceProvider.GetService<IProviderResultsRepository>().Should().NotBeNull(nameof(IProviderResultsRepository));
                scope.ServiceProvider.GetService<ITestResultsSearchService>().Should().NotBeNull(nameof(ITestResultsSearchService));
                scope.ServiceProvider.GetService<ITestResultsCountsService>().Should().NotBeNull(nameof(ITestResultsCountsService));
                scope.ServiceProvider.GetService<ITestResultsService>().Should().NotBeNull(nameof(ITestResultsService));
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
                { "calcsClient:ApiEndpoint", "https://localhost:7002/api/" },
                { "calcsClient:ApiKey", "Local" },
                { "scenariosClient:ApiEndpoint", "https://localhost:7006/api/" },
                { "scenariosClient:ApiKey", "Local" },
                { "resultsClient:ApiEndpoint", "https://localhost:7005/api/" },
                { "resultsClient:ApiKey", "Local" },
            };

            return configData;
        }
    }
}
