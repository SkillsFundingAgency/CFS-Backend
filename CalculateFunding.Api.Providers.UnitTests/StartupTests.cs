using CalculateFunding.Api.Providers.Controllers;
using CalculateFunding.Services.Providers;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Api.Providers.UnitTests
{
    [TestClass]
    public class StartupTests : IoCUnitTestBase
    {
        bool enableMajorMinorVersioning = true;

        [TestInitialize()]
        public void BeforeTest()
        {
            enableMajorMinorVersioning = true;
        }

        [TestMethod]
        public void ConfigureServices_RegisterDependenciesCorrectly()
        {
            // Arrange
            IConfigurationRoot configuration = CreateTestConfiguration();
            Startup target = new Startup(configuration);

            // Act
            target.ConfigureServices(Services);

            // Assert
            ResolveType<MasterProviderController>().Should().NotBeNull(nameof(MasterProviderController));
            ResolveType<ProviderByDateController>().Should().NotBeNull(nameof(ProviderByDateController));
            ResolveType<ProviderByVersionController>().Should().NotBeNull(nameof(ProviderByVersionController));
            ResolveType<ScopedProvidersController>().Should().NotBeNull(nameof(ProviderByVersionController));
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

            ProviderVersionService service = (ProviderVersionService)serviceProvider.GetService(typeof(IProviderVersionService));

            service
              .Should()
              .BeOfType<ProviderVersionService>();
        }

        [TestMethod]
        public void ConfigureServices_WhenMajorMinorVersioningIsDisabled_RegisterDependenciesCorrectly()
        {
            // Arrange
            enableMajorMinorVersioning = false;

            IConfigurationRoot configuration = CreateTestConfiguration();

            Startup target = new Startup(configuration);

            // Act
            target.ConfigureServices(Services);

            // Assert
            IServiceProvider serviceProvider = target.ServiceProvider;

            ProviderVersionService service = (ProviderVersionService)serviceProvider.GetService(typeof(IProviderVersionService));

            service
               .Should()
               .BeOfType<ProviderVersionService>();
        }

        protected override Dictionary<string, string> AddToConfiguration()
        {
            var configData = new Dictionary<string, string>
            {
                { "SearchServiceName", "ss-t1te-cfs"},
                { "SearchServiceKey", "test" },
                { "CosmosDbSettings:CollectionName", "calcs" },
                { "CosmosDbSettings:DatabaseName", "calculate-funding" },
                { "CosmosDbSettings:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=dGVzdA==;" },
                { "specificationsClient:ApiEndpoint", "https://localhost:7001/api/" },
                { "specificationsClient:ApiKey", "Local" },
                { "resultsClient:ApiEndpoint", "https://localhost:7005/api/" },
                { "resultsClient:ApiKey", "Local" },
                { "datasetsClient:ApiEndpoint", "https://localhost:7004/api/" },
                { "datasetsClient:ApiKey", "Local" },
                { "jobsClient:ApiEndpoint", "https://localhost:7010/api/" },
                { "jobsClient:ApiKey", "Local" },
                { "CommonStorageSettings:ConnectionString", "StorageConnection" }
            };

            return configData;
        }
    }
}